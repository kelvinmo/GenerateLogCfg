using System.Collections.Generic;

namespace GenerateLogCfg
{
    /*
     * Simple command line parser based on 
     * http://dotnetfollower.com/wordpress/2012/03/c-simple-command-line-arguments-parser/
     */
    class CommandLineArguments
    {
        protected readonly string prefix;
        protected Dictionary<string, string> parsed = new Dictionary<string, string>();
        protected int unnamed = 0;

        public CommandLineArguments(string[] args, string prefix)
        {
            this.prefix = prefix;

            if ((args != null) && (args.Length > 0))
            {
                Parse(args);
            }
        }

        public CommandLineArguments(string[] args) : this(args, "--")
        {
            
        }

        public string this[string key]
        {
            get { return GetValue(key); }
            set { if (key != null) parsed[key] = value; }
        }
        public string Prefix
        {
            get { return prefix; }
        }
        public int UnnamedCount
        {
            get { return unnamed; }
        }

        public bool Contains(string key)
        {
            string adjustedKey;
            return ContainsKey(key, out adjustedKey);
        }

        public virtual string GetOptionName(string key)
        {
            return IsOption(key) ? key.Substring(this.prefix.Length) : key;
        }
        public virtual string GetOption(string key)
        {
            return !IsOption(key) ? (this.prefix + key) : key;
        }

        public virtual bool IsOption(string s)
        {
            return s.StartsWith(this.prefix);
        }

        protected virtual void Parse(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null) continue;

                string key = null;
                string val = null;

                if (IsOption(args[i]))
                {
                    key = args[i];

                    if (i + 1 < args.Length && !IsOption(args[i + 1]))
                    {
                        val = args[i + 1];
                        i++;
                    }
                }
                else
                    val = args[i];

                // adjustment - unnamed argument
                if (key == null)
                {
                    unnamed++;
                    key = unnamed.ToString();
                }
                parsed[key] = val;
            }
        }

        protected virtual string GetValue(string key)
        {
            string adjustedKey;
            if (ContainsKey(key, out adjustedKey))
                return parsed[adjustedKey];

            return null;
        }

        protected virtual bool ContainsKey(string key, out string adjustedKey)
        {
            adjustedKey = key;

            if (parsed.ContainsKey(key))
                return true;

            if (IsOption(key))
            {
                string peeledKey = GetOptionName(key);
                if (parsed.ContainsKey(peeledKey))
                {
                    adjustedKey = peeledKey;
                    return true;
                }
                return false;
            }

            string decoratedKey = GetOption(key);
            if (parsed.ContainsKey(decoratedKey))
            {
                adjustedKey = decoratedKey;
                return true;
            }
            return false;
        }
    
    }
}
