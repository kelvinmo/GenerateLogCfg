/*
 * GenerateLogCfg
 * 
 * Copyright (C) Kelvin Mo 2016
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above
 *    copyright notice, this list of conditions and the following
 *    disclaimer in the documentation and/or other materials provided
 *    with the distribution.
 * 
 * 3. The name of the author may not be used to endorse or promote
 *    products derived from this software without specific prior
 *    written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS
 * OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
 * IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN
 * IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using SimpleCmd;

namespace GenerateLogCfg
{
    class Program
    {
        protected string ecuid = null;
        protected string protocol = "ssmk";
        protected string trigger = "defogger";

        protected XmlDocument defs;

        protected IDictionary<string, Parameter> parameters = new SortedDictionary<string, Parameter>(Comparer<string>.Create((a, b) => {
            // 1st char determines if it is a regular or ECU Param (reg params come first)
            if (a[0] == 'P' && b[0] == 'E')
		        return -1;
	        else if (a[0] == 'E' && b[0] == 'P')
		        return 1;
	        else {
                return StringComparer.InvariantCulture.Compare(a, b);
            }
        }));

        protected List<string> sorted = new List<string>();

        public Program()
        {

        }

        public XmlDocument Definitions
        {
            get { return defs; }
            set { this.defs = value; }
        }

        public string ECUID
        {
            get { return ecuid; }
            set { this.ecuid = value; }
        }

        public string Trigger
        {
            get { return trigger; }
            set { this.trigger = value; }
        }

        public string Protocol
        {
            get { return protocol; }
            set { this.protocol = value; }
        }

        public void ConvertProfile(XmlDocument profile, TextWriter output, TextWriter err)
        {
            // Protocol
            XmlElement protocolNode = (XmlElement) profile.SelectSingleNode("/profile");
            protocolNode.GetAttribute("protocol");

            // Parameters
            XmlNodeList paramNodes = profile.SelectNodes("/profile/parameters/parameter[@livedata = 'selected']");
            foreach (XmlElement paramNode in paramNodes)
            {
                if (paramNode.Attributes["id"] == null)
                {
                    err.WriteLine("XML error: id attribute missing in profile");
                    continue;
                }
                if (paramNode.Attributes["units"] == null)
                {
                    err.WriteLine("XML error: units attribute missing in parameter " + paramNode.Attributes["id"].Value);
                    continue;
                }

                string key = String.Format("{0}:{1}", paramNode.Attributes["id"].Value, paramNode.Attributes["units"].Value);
                if (parameters.ContainsKey(key))
                {
                    parameters[key].Hidden = false;
                }
                else
                {
                    parameters[key] = new Parameter(paramNode.Attributes["id"].Value, paramNode.Attributes["units"].Value) { Hidden = false };
                }
                
            }

            // Add dependencies
            foreach (string key in parameters.Keys)
            {
                ParseParameter(parameters[key], err);
            }

            // Topological sort
            SortParameters(err);

            // Write output
            WriteOutput(output);
        }

        protected void ParseParameter(Parameter p, TextWriter err)
        {
            // Get definition
            XmlElement def = (XmlElement) defs.SelectSingleNode(String.Format("//*[@id='{0}']", p.ID));
            if (def == null)
            {
                err.WriteLine("Parameter definition not found: " + p.ID);
                return;
            }
            XmlElement formulaNode = (XmlElement) def.SelectSingleNode(String.Format("conversions/conversion[@units='{0}']", p.Units));
            if (formulaNode == null)
            {
                err.WriteLine(String.Format("Unit not found for parameter {0}: {1}", p.ID, p.Units));
                return;
            }

            // Populate parameter
            if (def.HasAttribute("name"))
                p.Name = def.GetAttribute("name");
            else
                p.Name = p.ID;

            p.Expression = new Expression(formulaNode.GetAttribute("expr"));

            if (p.ID[0] == 'P')
            {
                if (def["address"] != null)
                {
                    p.ParamID = def["address"].InnerText;
                    if (def["address"].HasAttribute("length"))
                    {
                        p.DataBits = Convert.ToUInt32(def["address"].GetAttribute("length")) * 8;
                    }
                }

                if (def["depends"] != null)
                {
                    XmlNodeList dependencies = def["depends"].SelectNodes("ref");
                    foreach (XmlElement reference in dependencies)
                    {
                        string refkey;
                        string refid = reference.GetAttribute("parameter");

                        // How many units are there in refid?
                        XmlNodeList refunits = defs.SelectNodes(String.Format("//*[@id='{0}'/conversions/conversion", refid));

                        if (refunits.Count == 0)
                        {
                            err.WriteLine(String.Format("Dependency not found for {0}: {1}", p.ID, refid));
                            continue;
                        }
                        else if (refunits.Count == 1)
                        {
                            p.Expression.References[refid] = ((XmlElement)refunits[0]).GetAttribute("units");
                        }

                        refkey = String.Format("{0}:{1}", refid, p.Expression.References[refid]);

                        if (!parameters.ContainsKey(refkey))
                        {
                            parameters[refkey] = new Parameter(refid, p.Expression.References[refid]);
                            ParseParameter(parameters[refkey], err);
                        }
                    }
                }
            }
            else if (p.ID[0] == 'E')
            {
                if (ecuid == null)
                {
                    err.WriteLine(String.Format("ECUID required - specify using --ecu-id: {0}", p.ID));
                    return;
                }

                XmlElement ecuNode = (XmlElement) def.SelectSingleNode(String.Format("ecu[contains(@id,'{0}')]", ecuid));
                if (ecuNode == null)
                {
                    err.WriteLine(String.Format("Definition not found for ECUID: {0}", p.ID));
                    return;
                }

                if (ecuNode["address"] != null)
                {
                    p.ParamID = ecuNode["address"].InnerText;
                    if (formulaNode.HasAttribute("storagetype") && (formulaNode.GetAttribute("storagetype") == "float"))
                    {
                        p.IsFloat = true;
                    }
                    else if (ecuNode["address"].HasAttribute("length"))
                    {
                        p.DataBits = Convert.ToUInt32(ecuNode["address"].GetAttribute("length")) * 8;
                    }
                }
            }            
        }

        protected void SortParameters(TextWriter err)
        {
            Dictionary<string, bool> visited = new Dictionary<string, bool>();

            foreach (string key in parameters.Keys)
            {
                SortVisitParameter(key, visited);
            }
        }

        private void SortVisitParameter(string key, Dictionary<string, bool> visited)
        {
            bool inProcess;
            var alreadyVisited = visited.TryGetValue(key, out inProcess);

            if (alreadyVisited)
            {
                if (inProcess)
                {
                    throw new ArgumentException("Cyclic dependency found.");
                }
            }
            else
            {
                visited[key] = true;

                if (parameters[key].Expression.References.Count > 0)
                {
                    foreach (var dependency in parameters[key].Expression.References.Keys)
                    {
                        SortVisitParameter(dependency, visited);
                    }
                }

                visited[key] = false;
                sorted.Add(key);
            }
        }

        protected void WriteOutput(TextWriter output)
        {
            Parameter param;

            output.NewLine = "\n";

            output.WriteLine(";");
            output.WriteLine("; Generated by GenerateLogCfg");
            output.WriteLine("; ()");
            output.WriteLine(";");

            // Type
            output.WriteLine("; --- General ------------------------------------------------");
            output.WriteLine(String.Format("type = {0}", protocol));
            output.WriteLine();

            // Parameters
            output.WriteLine("; --- Parameters ---------------------------------------------");
            foreach (string key in sorted)
            {
                param = parameters[key];

                output.WriteLine("; " + param.ID + " - " + param.Name + " (" + param.Units + ")");
                output.WriteLine(param.ToString());
            }

            // Add parameters for defogger [S20]
            if (trigger == "defogger")
            {
                output.WriteLine("; Defogger Switch Trigger");
                output.WriteLine("paramname = defogger_trigger");
                output.WriteLine("paramid = 0x64");
                output.WriteLine("databits = 1");
                output.WriteLine("offsetbits = 5");
                output.WriteLine("isvisible = 0");
                output.WriteLine();
            }

            // Trigger
            output.WriteLine("; --- Triggers -----------------------------------------------");
            if (trigger == "defogger")
            {
                output.WriteLine("; Start log when defogger_trigger == 1");
                output.WriteLine("conditionrpn = defogger_trigger,1,==");
                output.WriteLine("action = start");
                output.WriteLine();

                output.WriteLine("; Stop log when defogger_trigger == 1");
                output.WriteLine("conditionrpn = defogger_trigger,0,==");
                output.WriteLine("action = stop");
                output.WriteLine();
            }
            else if (trigger == "engine")
            {
                output.WriteLine("; Start log when RPM > 0");
                output.WriteLine("conditionrpn = Engine_Speed(rpm),0,>");
                output.WriteLine("action = start");
                output.WriteLine();

                output.WriteLine("; Stop log when defogger_trigger == 1");
                output.WriteLine("conditionrpn = Engine_Speed(rpm),0,==");
                output.WriteLine("action = stop");
                output.WriteLine();
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("GenerateLogCfg [OPTIONS] DEFINITIONS [PROFILE] [OUTPUT]");
            Console.WriteLine();
            Console.WriteLine("    DEFINITIONS    RomRaider Logger definitions file");
            Console.WriteLine("    PROFILE        RomRaider Logger profile, defaults to STDIN");
            Console.WriteLine("    OUTPUT         Tactrix logcfg, defaults to STDOUT");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("    --ecu-id ECUID              ECU ID for extended parameters");
            Console.WriteLine("    --trigger defogger|engine   Log start/stop trigger (default: defogger)");
            Console.WriteLine("    --protocol ssmk|ssmcan      Logging protocol (default: ssmk)");
        }

        static void Main(string[] args)
        {
            XmlDocument defs = new XmlDocument();
            defs.XmlResolver = null;

            XmlDocument profile = new XmlDocument();
            profile.XmlResolver = null;

            TextWriter output;
            TextWriter err = Console.Error;

            SimpleCmdParser parser = new SimpleCmdParser();
            parser.Add("help");
            parser.Add("ecu-id", true);
            parser.Add("trigger", true);
            parser.Add("protocol", true);

            SimpleCmdResults results = parser.Parse(args);

            if (results.Contains("help"))
            {
                ShowUsage();
                return;
            }

            if (results.Contains("@1"))
            {
                defs.Load((string)results["@1"]);
            } else {
                Console.Error.WriteLine("Profile not specified");
                Console.WriteLine();
                ShowUsage();
                return;
            }

            if (results.Contains("@2"))
            {
                profile.Load((string)results["@2"]);
            } else {
                profile.Load(Console.In);
            }

            if (results.Contains("@3"))
            {
                try
                {
                    output = File.CreateText((string)results["@3"]);
                } catch (IOException e) {
                    Console.Error.WriteLine(e.Message + ": " + results["@3"]);
                    return;
                }
            } else {
                output = Console.Out;
            }

            Program p = new Program();
            p.Definitions = defs;

            if (results.Contains("ecu-id")) p.ECUID = (string)results["ecu-id"];
            if (results.Contains("trigger")) p.Trigger = (string)results["trigger"];
            if (results.Contains("protocol")) p.Protocol = (string)results["protocol"];

            p.ConvertProfile(profile, output, err);

            output.Close();
        }
    }
}
