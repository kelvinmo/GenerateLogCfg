/*
 * GenerateLogCfg
 * 
 * Copyright(C) Kelvin Mo 2016
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
using System.Globalization;
using System.Text.RegularExpressions;

namespace GenerateLogCfg
{
    class Expression
    {
        protected string[] tokens;
        protected IDictionary<string, string> refs = new Dictionary<string, string>();

        public Expression(string expr)
        {
            Tokenize(expr);
            ParseReferences();
        }

        public IDictionary<string, string> References
        {
            get { return refs; }
        }

        public string[] ToRPN()
        {
            return ReversePolishNotation.InfixToRPN(tokens);
        }

        private void Tokenize(string expr)
        {
            expr = expr.Trim().Replace(" ", "");

            string buffer = string.Empty;
            List<string> list = new List<string>();

            foreach (char c in expr)
            {
                string s = c.ToString(CultureInfo.InvariantCulture);
                if (ReversePolishNotation.IsOperator(s) || (c == '(') || (c == ')'))
                {
                    if (buffer.Length > 0) list.Add(buffer);
                    list.Add(s);
                    buffer = string.Empty;
                }
                else
                {
                    buffer += c;
                }
            }
            if (buffer.Length > 0) list.Add(buffer);

            tokens = list.ToArray();
        }

        private void ParseReferences()
        {
            foreach (string token in tokens)
            {
                // Skip if a number
                if (ReversePolishNotation.IsOperator(token) || (token == "(") || (token == ")")) continue;

                // Skip if an operator
                double isNumeric;
                if (Double.TryParse(token, out isNumeric)) continue;

                // Skip if just x (logging value)
                if (token == "x") continue;

                MatchCollection matches = Regex.Matches(token, @"\[([A-Za-z0-9_]):([A-Za-z0-9])\]");
                if (matches.Count == 0)
                {
                    // Simple reference
                    refs[token] = null;
                }
                else
                {
                    // References with units
                    refs[matches[1].Value] = matches[2].Value;
                }
            }
        }
    }
}
