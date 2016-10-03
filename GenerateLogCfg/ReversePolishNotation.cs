using System;
using System.Collections.Generic;


//http://andreinc.net/2010/10/05/converting-infix-to-rpn-shunting-yard-algorithm/

namespace GenerateLogCfg
{
    class ReversePolishNotation
    {
        // Associativity constants for operators
        private const int LEFT_ASSOC = 0;
        private const int RIGHT_ASSOC = 1;

        // Supported operators
        private static readonly IReadOnlyDictionary<string, int[]> OPERATORS = new Dictionary<string, int[]>()
        {
            { "+", new int[] { 0, LEFT_ASSOC } },
            { "-", new int[] { 0, LEFT_ASSOC } },
            { "*", new int[] { 5, LEFT_ASSOC } },
		    { "/", new int[] { 5, LEFT_ASSOC } },
		    { "%", new int[] { 5, LEFT_ASSOC } },
		    { "^", new int[] { 10, RIGHT_ASSOC } }
        };

	    /**
	     * Test if a certain is an operator .
	     * @param token The token to be tested .
	     * @return True if token is an operator . Otherwise False .
	     */
	    public static bool IsOperator(string token)
        {
            return OPERATORS.ContainsKey(token);
        }

        /**
         * Test the associativity of a certain operator token .
         * @param token The token to be tested (needs to operator).
         * @param type LEFT_ASSOC or RIGHT_ASSOC
         * @return True if the tokenType equals the input parameter type .
         */
        private static bool isAssociative(string token, int type)
        {
            if (!IsOperator(token))
            {
                throw new ArgumentException("Invalid token: " + token);
            }
            if (OPERATORS[token][1] == type)
            {
                return true;
            }
            return false;
        }

        /**
         * Compare precendece of two operators.
         * @param token1 The first operator .
         * @param token2 The second operator .
         * @return A negative number if token1 has a smaller precedence than token2,
         * 0 if the precendences of the two tokens are equal, a positive number
         * otherwise.
         */
        private static int cmpPrecedence(string token1, string token2)
        {
            if (!IsOperator(token1) || !IsOperator(token2))
            {
                throw new ArgumentException("Invalied tokens: " + token1  + " " + token2);
            }
            return OPERATORS[token1][0] - OPERATORS[token2][0];
        }

        public static String[] infixToRPN(string[] inputTokens)
        {
            List<string> output = new List<string>();
            Stack<string> stack = new Stack<string>();
            // For all the input tokens [S1] read the next token [S2]
            foreach (string token in inputTokens)
            {
                if (IsOperator(token))
                {
                    // If token is an operator (x) [S3]
                    while ((stack.Count > 0) && IsOperator(stack.Peek()))
                    {
                        // [S4]
                        if ((isAssociative(token, LEFT_ASSOC) && cmpPrecedence(token, stack.Peek()) <= 0)
                            || (isAssociative(token, RIGHT_ASSOC) && cmpPrecedence(token, stack.Peek()) < 0))
                        {
						    output.Add(stack.Pop());   // [S5] [S6]
                            continue;
                        }
                        break;
                    }
                    // Push the new operator on the stack [S7]
                    stack.Push(token);
                }
                else if (token == "(")
                {
				    stack.Push(token); 	// [S8]
			    }
                else if (token == ")")
                {
				    // [S9]
				    while ((stack.Count > 0) && (stack.Peek() != "("))
                    {
					    output.Add(stack.Pop()); // [S10]
				    }
				    stack.Pop(); // [S11]
			    } else {
				    output.Add(token); // [S12]
			    }
		    }
		    while (stack.Count > 0) {
			    output.Add(stack.Pop()); // [S13]
		    }
		    return output.ToArray();
	    }
    }
}
