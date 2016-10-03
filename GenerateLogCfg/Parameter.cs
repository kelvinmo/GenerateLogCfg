using System;
using System.Collections.Generic;
using System.IO;

namespace GenerateLogCfg
{
    class Parameter
    {
        protected string id;
        protected string units;
        protected string name;
        protected string paramid;
        protected uint databits = 0;
        protected bool hidden = true;
        protected bool isFloat = false;  // if id starts if 'E'
        protected Expression expression = null;
        protected List<string> dependencies = new List<string>();

        public Parameter(string id, string units)
        {
            this.id = id;
            this.units = units;
        }

        public string ID
        {
            get { return this.id; }
        }

        public string Units
        {
            get { return this.units; }
        }

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public string ParamID
        {
            get { return this.paramid; }
            set { this.paramid = value; }
        }

        public uint DataBits
        {
            get { return this.databits; }
            set { this.databits = value; }
        }

        public bool Hidden
        {
            get { return this.hidden; }
            set { this.hidden = value; }
        }

        public bool IsFloat
        {
            get { return this.isFloat; }
            set { this.isFloat = value; }
        }

        public Expression Expression
        {
            get { return this.expression; }
            set { this.expression = value; }
        }

        public string GetCSVFieldName()
        {
            return string.Format("{0} ({1})", name, units).Replace(' ', '_');
        }

        public override string ToString()
        {
            StringWriter sw = new StringWriter();
            sw.NewLine = "\n";

            sw.Write("paramname = ");
            sw.WriteLine(GetCSVFieldName());

            if (paramid != null)
            {
                sw.Write("paramid = ");
                sw.WriteLine(paramid);

                if (databits > 0)
                {
                    sw.Write("databits = ");
                    sw.WriteLine(databits);
                }

                if (id[0] == 'E' && isFloat) sw.WriteLine("isfloat = 1");

                if (expression != null)
                {
                    sw.Write("scalingrpn = ");
                    sw.WriteLine(String.Join(",", expression.ToRPN()));
                }

                if (hidden) sw.WriteLine("isvisible = 0");
            }
            
            return sw.ToString();
        }
    }
}
