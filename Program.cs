using Microsoft.Win32;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using System.Linq;

namespace scePa
{
    class Program
    {
        static void Main(string[] args)
        {
            var baseDir = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Illusion\\HoneySelect\\", "INSTALLDIR", -1).ToString();
            if (baseDir == "-1") return;
            var di_base = new DirectoryInfo(baseDir);
            if (!di_base.Exists) return;
            var path32 = Path.Combine(di_base.FullName, "HoneySelect_32_Data", "Managed", "Assembly-CSharp.dll");
            var path64 = Path.Combine(di_base.FullName, "HoneySelect_64_Data", "Managed", "Assembly-CSharp.dll");

            try
            {
                Patch(path32);
            }
            catch (Exception)
            {
                Console.WriteLine("Error patching.");
            }
            try
            {
                Patch(path64);
            }
            catch (Exception)
            {
                Console.WriteLine("Error patching.");
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        static void Patch(string path)
        {
            Console.WriteLine("Patching: " + path);
            var target = path;
            var backup = target + ".scePa_backup";

            var defaultAssemblyResolver = new DefaultAssemblyResolver();
            var parameters = new ReaderParameters
            {
                AssemblyResolver = defaultAssemblyResolver
            };
            defaultAssemblyResolver.AddSearchDirectory(new FileInfo(path).Directory.FullName);

            if (!File.Exists(backup))
            {
                File.Copy(target, backup);
                File.Delete(target);
            }

            if (!File.Exists(backup))
            {
                Console.WriteLine("DLL not found.");
                return;
            }

            var md = ModuleDefinition.ReadModule(backup, parameters);

            //Remove events
            var adv = md.GetType("Manager.ADV");
            var cex = adv.Methods;
            var ce = adv.Methods.First((MethodDefinition x) => x.Name == "CheckEvent" && x.ReturnType.FullName.ToString() == "System.Int32");
            var ce_ilp = ce.Body.GetILProcessor();
            var ce_first = ce.Body.Instructions[0];
            if (ce_first.OpCode.ToString() != "ldc.i4.m1")
            {
                ce.Body.Instructions.Clear();
                ce_ilp.Emit(OpCodes.Ldc_I4_M1);
                ce_ilp.Emit(OpCodes.Ret);
                Console.WriteLine("Patched CheckEvent");
            }

            //Remove events
            var ce2 = adv.Methods.First((MethodDefinition x) => x.Name == "CheckEvent" && x.ReturnType.FullName.ToString() == "System.Boolean" && x.Parameters.Count == 4);
            var ce2_ilp = ce2.Body.GetILProcessor();
            var ce2_first = ce2.Body.Instructions[0];
            if (ce2_first.OpCode.ToString() != "ldc.i4.0")
            {
                ce2.Body.ExceptionHandlers.Clear();
                ce2.Body.Variables.Clear();
                ce2.Body.Instructions.Clear();
                ce2_ilp.Emit(OpCodes.Ldc_I4_0);
                ce2_ilp.Emit(OpCodes.Ret);
                Console.WriteLine("Patched CheckEvent");
            }

            var hs = md.GetType("HScene");

            //Remove state animation filter.
            var mdos = hs.Methods.First(x => x.Name == "MotionDecisionOfState");
            var mdos_ilp = mdos.Body.GetILProcessor();
            var mdos_first = mdos.Body.Instructions[0];
            if (mdos_first.OpCode.ToString() == "ldarg.0") //return true;
            {
                mdos.Body.Instructions.Clear();
                mdos_ilp.Emit(OpCodes.Ldc_I4_1);
                mdos_ilp.Emit(OpCodes.Ret);
                Console.WriteLine("Patched MotionDecisionOfState");
            }

            //Remove map animation filter.
            var mdom = hs.Methods.First(x => x.Name == "MotionDecisionOfMap");
            var mdom_ilp = mdom.Body.GetILProcessor();
            var mdom_first = mdom.Body.Instructions[0];
            if (mdom_first.OpCode.ToString() == "ldarg.1")  //return true;
            {
                mdom.Body.Instructions.Clear();
                mdom_ilp.Emit(OpCodes.Ldc_I4_1);
                mdom_ilp.Emit(OpCodes.Ret);
                Console.WriteLine("Patched MotionDecisionOfMap");
            }

            //Patch flag to load animation info.
            var clafn = hs.Methods.First(x => x.Name == "CreateListAnimationFileName");
            var clafn_ilp = clafn.Body.GetILProcessor();
            var clafn_flag3 = clafn.Body.Instructions[692]; //bool flag3 = false -> true
            if (clafn_flag3.OpCode.ToString() == "ldc.i4.0")
            {
                clafn_flag3.OpCode = OpCodes.Ldc_I4_1;
                Console.WriteLine("Patched CreateListAnimationFileName");
            }

            md.Write(target);
        }
    }
}
