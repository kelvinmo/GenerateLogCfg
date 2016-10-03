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
            return ReversePolishNotation.infixToRPN(tokens);
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
