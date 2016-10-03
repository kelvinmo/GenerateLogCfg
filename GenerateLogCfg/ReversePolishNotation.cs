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

namespace GenerateLogCfg
{
    /*
     * Infix to RPN converter based on
     * http://andreinc.net/2010/10/05/converting-infix-to-rpn-shunting-yard-algorithm/
     */
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

        public static bool IsOperator(string token)
        {
            return OPERATORS.ContainsKey(token);
        }

        private static bool IsAssociative(string token, int type)
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

        private static int ComparePrecedence(string token1, string token2)
        {
            if (!IsOperator(token1) || !IsOperator(token2))
            {
                throw new ArgumentException("Invalied tokens: " + token1  + " " + token2);
            }
            return OPERATORS[token1][0] - OPERATORS[token2][0];
        }

        public static string[] InfixToRPN(string[] inputTokens)
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
                        if ((IsAssociative(token, LEFT_ASSOC) && ComparePrecedence(token, stack.Peek()) <= 0)
                            || (IsAssociative(token, RIGHT_ASSOC) && ComparePrecedence(token, stack.Peek()) < 0))
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
