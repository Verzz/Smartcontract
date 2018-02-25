using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo;
using Neo.VM;
using Neo.Cryptography;
using System.IO;
using System.Numerics;
using Newtonsoft.Json;

namespace ConsoleApp1
{
    class Program
    {
        private static ExecutionEngine engine = null;

        static void Main(string[] args)
        {

            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Welcome to the Verzz-console tester");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("\n");

            string request = "";
            string type = "";

            while (request != "exit")
            {
                try
                {
                    Console.WriteLine("Please enter an operation");
                    request = Console.ReadLine();
                    Console.WriteLine("Please enter a response type");
                    type = Console.ReadLine();

                    Console.WriteLine("\n");
                    Console.WriteLine("-----------------------------------");

                    switch (type)
                    {
                        case "boolean":
                            Console.WriteLine($"-> Result : {DoAction(request).GetBoolean()}");
                            break;
                        case "bigint":
                            Console.WriteLine($"-> Result : {DoAction(request).GetBigInteger()}");
                            break;
                        case "string":
                            Console.WriteLine($"-> Result : {DoAction(request).GetString()}");
                            break;
                        case "byte":
                            Console.WriteLine($"-> Result : {DoAction(request).GetByteArray()}");
                            break;
                        default:
                            Console.WriteLine("-> Exception : Unknown Request.");
                            break;
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("-> Exception : A problem occured with your request.");
                }
                finally
                {
                    Console.WriteLine("-----------------------------------");
                    Console.WriteLine("\n");
                }
            }
        }

        static StackItem DoAction(string request)
        {
            engine = new ExecutionEngine(null, Crypto.Default);
            engine.LoadScript(File.ReadAllBytes("PUTYOURAVMHERE"));

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(request);
                sb.EmitPush(request);
                engine.LoadScript(sb.ToArray());
            }
            engine.Execute(); // start execution
            return engine.EvaluationStack.Peek();
        }
    }
}
