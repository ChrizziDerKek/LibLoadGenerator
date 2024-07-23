using System;
using System.Collections.Generic;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.CodeDom;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace LibLoadGenerator
{
    class Program
    {
        static void CopyTo(Stream src, Stream dst)
        {
            byte[] bytes = new byte[4096];
            int cnt;
            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
                dst.Write(bytes, 0, cnt);
        }
        static byte[] Zip(byte[] data)
        {
            using (var msi = new MemoryStream(data))
            {
                using (var mso = new MemoryStream())
                {
                    using (var gs = new GZipStream(mso, CompressionMode.Compress))
                    {
                        CopyTo(msi, gs);
                    }
                    return mso.ToArray();
                }
            }
        }
        static string ToCompressedBase64(string dll)
        {
            if (!File.Exists(dll))
                return "";
            return Convert.ToBase64String(Zip(File.ReadAllBytes(dll)));
        }
        static void Main(string[] args)
        {
            List<string> dlls = new List<string>();
            if (args.Length == 0)
                return;
            foreach (string a in args)
                dlls.Add(a);
            CodeCompileUnit compiler = new CodeCompileUnit();
            CodeNamespace space = new CodeNamespace("LibLoader");
            compiler.Namespaces.Add(space);

            space.Imports.Add(new CodeNamespaceImport("System"));
            space.Imports.Add(new CodeNamespaceImport("System.IO"));
            space.Imports.Add(new CodeNamespaceImport("System.IO.Compression"));
            space.Imports.Add(new CodeNamespaceImport("System.Reflection"));

            CodeTypeDeclaration decl = new CodeTypeDeclaration("Loader") { IsClass = true };
            space.Types.Add(decl);

            CodeMemberMethod meth = new CodeMemberMethod { Attributes = MemberAttributes.Public };
            meth.Attributes |= MemberAttributes.Static;
            meth.Attributes |= MemberAttributes.Final;
            meth.Name = "Load";
            meth.Statements.Add(new CodeAttachEventStatement(new CodeEventReferenceExpression(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(AppDomain)), "CurrentDomain"), "AssemblyResolve"), new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("Loader"), "Handler")));
            decl.Members.Add(meth);

            meth = new CodeMemberMethod();
            meth.Attributes |= MemberAttributes.Static;
            meth.Attributes |= MemberAttributes.Private;
            meth.Attributes |= MemberAttributes.Final;
            meth.Name = "Handler";
            meth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "sender"));
            meth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ResolveEventArgs), "args"));
            meth.ReturnType = new CodeTypeReference(typeof(Assembly));
            meth.Statements.Add(new CodeVariableDeclarationStatement(typeof(string), "dll", new CodeArrayIndexerExpression(new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("args"), "Name"), "Split", new CodePrimitiveExpression(',')), new CodePrimitiveExpression(0))));
            meth.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("dll"), new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("dll"), "ToLower")));
            foreach (string dll in dlls)
            {
                meth.Statements.Add(new CodeConditionStatement(new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("dll"), CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(dll.ToLower().Replace(".dll", ""))),
                    new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Assembly)), "Load", new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Loader"), "Unpack", new CodePrimitiveExpression(ToCompressedBase64(dll)))))));
            }
            meth.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
            decl.Members.Add(meth);

            meth = new CodeMemberMethod();
            meth.Attributes |= MemberAttributes.Static;
            meth.Attributes |= MemberAttributes.Private;
            meth.Attributes |= MemberAttributes.Final;
            meth.Name = "Unpack";
            meth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "base64dll"));
            meth.ReturnType = new CodeTypeReference(typeof(byte[]));
            meth.Statements.Add(new CodeVariableDeclarationStatement(typeof(byte[]), "data", new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("System.Convert"), "FromBase64String", new CodeVariableReferenceExpression("base64dll"))));
            meth.Statements.Add(new CodeVariableDeclarationStatement(typeof(MemoryStream), "msi", new CodeObjectCreateExpression(typeof(MemoryStream), new CodeVariableReferenceExpression("data"))));
            meth.Statements.Add(new CodeVariableDeclarationStatement(typeof(MemoryStream), "mso", new CodeObjectCreateExpression(typeof(MemoryStream))));
            meth.Statements.Add(new CodeVariableDeclarationStatement(typeof(GZipStream), "gs", new CodeObjectCreateExpression(typeof(GZipStream), new CodeVariableReferenceExpression("msi"), new CodeCastExpression(typeof(CompressionMode), new CodePrimitiveExpression(0)))));
            meth.Statements.Add(new CodeVariableDeclarationStatement(typeof(byte[]), "bytes", new CodeArrayCreateExpression(typeof(byte[]), 0x1000)));
            meth.Statements.Add(new CodeIterationStatement(new CodeVariableDeclarationStatement(typeof(int), "i", new CodePrimitiveExpression(1)), new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(0)), new CodeAssignStatement(new CodeVariableReferenceExpression("_"), new CodeVariableReferenceExpression("i")),
                new CodeAssignStatement(new CodeVariableReferenceExpression("i"), new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("gs"), "Read", new CodeVariableReferenceExpression("bytes"), new CodePrimitiveExpression(0), new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("bytes"), "Length"))),
                new CodeConditionStatement(new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(0)), new CodeGotoStatement("again")),
                new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("mso"), "Write", new CodeVariableReferenceExpression("bytes"), new CodePrimitiveExpression(0), new CodeVariableReferenceExpression("i"))),
                new CodeLabeledStatement("again"),
                new CodeAssignStatement(new CodeVariableReferenceExpression("_"), new CodeVariableReferenceExpression("i"))
            ));
            meth.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("mso"), "ToArray")));
            decl.Members.Add(meth);

            CSharpCodeProvider provider = new CSharpCodeProvider();
            StringWriter sw = new StringWriter();
            IndentedTextWriter tw = new IndentedTextWriter(sw, "    ");
            provider.GenerateCodeFromCompileUnit(compiler, tw, new CodeGeneratorOptions());
            tw.Close();
            Console.WriteLine(sw.ToString());
            string file = "output.txt";
            if (!File.Exists(file))
                File.Create(file).Close();
            File.WriteAllText(file, sw.ToString());
            Console.ReadKey();
        }
    }
}