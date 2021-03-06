using NUnit.Framework;

namespace UnityScript2CSharp.Tests
{
    public partial class Tests
    {
        [Test]
        public void Self_Implicit()
        {
            //TODO: is it possible to get rid of the synthetic *self* ? (it is not marked as such)
            var sourceFiles = SingleSourceFor("self_implict.js", "public var a : int; function F() { a = 42; }");
            var expectedConvertedContents = SingleSourceFor("self_implict.cs", DefaultGeneratedClass + @"self_implict : MonoBehaviour { public int a; public virtual void F() { this.a = 42; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_Generic_Methods()
        {
            var sourceFiles = SingleSourceFor("simple_generic_method.js", "import UnityScript2CSharp.Tests; function F(o:NonGeneric) { return o.ToName.<NonGeneric>(42); }");
            var expectedConvertedFiles = SingleSourceFor("simple_generic_method.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + "simple_generic_method : MonoBehaviour { public virtual string F(NonGeneric o) { return o.ToName<NonGeneric>(42); } }");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [Test]
        public void Out_Ref_Parameters()
        {
            var sourceFiles = SingleSourceFor("out_ref_parameters.js", "import UnityScript2CSharp.Tests; function F() { var i:int; var j:int; j = 42; Methods.OutRef(i, j); }");
            var expectedConvertedFiles = SingleSourceFor("out_ref_parameters.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + "out_ref_parameters : MonoBehaviour { public virtual void F() { int i = 0; int j = 0; j = 42; Methods.OutRef(out i, ref j); } }");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [TestCase("true", "bool")]
        [TestCase("false", "bool")]
        [TestCase("4.2f", "float")]
        [TestCase("\"foo\"", "string")]
        [TestCase(@"""\""""", "string", TestName = "Double Quotes")]
        [TestCase(@"""\\""", "string", TestName = "Backslash")]
        public void Literal_Expressions(string literal, string expectedInferredCSType)
        {
            var sourceFiles = SingleSourceFor("literal_expressions.js", $"function F() {{ return {literal}; }}");
            var expectedConvertedFiles = SingleSourceFor("literal_expressions.cs", DefaultGeneratedClass +  $"literal_expressions : MonoBehaviour {{ public virtual {expectedInferredCSType} F() {{ return {literal}; }} }}");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [Test]
        public void Literal_Hashtable()
        {
            var sourceFiles = SingleSourceFor("literal_hash.js", "function F() { return {\"Id\": 42, \"Enabled\": false }; }");
            var expectedConvertedFiles = SingleSourceFor("literal_hash.cs", DefaultGeneratedClass + "literal_hash : MonoBehaviour { public virtual Hashtable F() { return new Hashtable() { {\"Id\", 42 }, {\"Enabled\", false }, }; } }");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [TestCase("i > 10", "true : false", "bool")]
        [TestCase("i == 10", "0 : 42", "int")]
        [TestCase("i == 10", "1.1f : 42.1f", "float")]
        [TestCase("(i > 0) && (i < 42)", "null : this", "ternary_operator")]
        [TestCase("i > 0 ? (i < 42", "1 : 2) : 3", "int")]
        //[TestCase("i > 10", "'A' : 'V'", "char")] // char -> string ?
        public void Ternary_Operator(string condition, string values, string inferredReturnTypeName)
        {
            var sourceFiles = SingleSourceFor("ternary_operator.js", $"function F(i:int) {{ return {condition} ? {values}; }}");
            var expectedConvertedFiles = SingleSourceFor("ternary_operator.cs", DefaultGeneratedClass + $"ternary_operator : MonoBehaviour {{ public virtual {inferredReturnTypeName} F(int i) {{ return {condition} ? {values}; }} }}");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [Test]
        public void Ternary_Operator_Casts()
        {
            var sourceFiles = SingleSourceFor("ternary_operator_cast.js", "function F(i:int, b: boolean, f: float): void { F(b ? i : f, b, i); }");
            var expectedConvertedFiles = SingleSourceFor("ternary_operator_cast.cs", DefaultGeneratedClass + "ternary_operator_cast : MonoBehaviour { public virtual void F(int i, bool b, float f) { this.F((int) (b ? i : f), b, i); } }");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [Test]
        public void Cast()
        {
            var sourceFiles = SingleSourceFor("cast.js", "function F(o:Object) { return o cast int; }");
            var expectedConvertedFiles = SingleSourceFor("cast.cs", DefaultGeneratedClass + "cast : MonoBehaviour { public virtual int F(object o) { return (int) o; } }");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [Test]
        public void As_Simple() //TryCastExpression
        {
            var sourceFiles = SingleSourceFor("as_simple.js", "function F(o:Object) { return o as as_simple; }");
            var expectedConvertedFiles = SingleSourceFor("as_simple.cs", DefaultGeneratedClass + "as_simple : MonoBehaviour { public virtual as_simple F(object o) { return o as as_simple; } }");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [Test]
        public void As_Complex() //TryCastExpression
        {
            var sourceFiles = SingleSourceFor("as_complex.js", "function F(o:Object) : Object { return (o as as_complex).F(o); }");
            var expectedConvertedFiles = SingleSourceFor("as_complex.cs", DefaultGeneratedClass + "as_complex : MonoBehaviour { public virtual object F(object o) { return (o as as_complex).F(o); } }");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [Test]
        public void Pre_Increment_Decrement()
        {
            var sourceFiles = SingleSourceFor("pre_increment_decrement.js", "function F(i:int) { return ++i + i++; }");
            var expectedConvertedContents = SingleSourceFor("pre_increment_decrement.cs", DefaultGeneratedClass + @"pre_increment_decrement : MonoBehaviour { public virtual int F(int i) { return ++i + i++; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("var j = i++; return j", "int j = i++; return j")]
        [TestCase("return i++", "return i++")]
        [TestCase("return i++ > 10 ? 1 : 0", "return i++ > 10 ? 1 : 0")]
        public void Post_Increment(string usExpression, string csExpression)
        {
            var sourceFiles = SingleSourceFor("post_increment.js", $"function F(i:int) {{ {usExpression}; }}");
            var expectedConvertedContents = SingleSourceFor("post_increment.cs", DefaultGeneratedClass + $"post_increment : MonoBehaviour {{ public virtual int F(int i) {{ {csExpression}; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void New_Expression()
        {
            var sourceFiles = SingleSourceFor("new_expression.js", "import System.Text; function F(o:Object) : StringBuilder { F(new StringBuilder()); return new StringBuilder(); }");
            var expectedConvertedContents = SingleSourceFor("new_expression.cs", "using System.Text; " + DefaultGeneratedClass + @"new_expression : MonoBehaviour { public virtual StringBuilder F(object o) { this.F(new StringBuilder()); return new StringBuilder(); } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Implicit_New_Expression([Values("Struct", "C")] string typeName, [Values("42", "")] string paramValue)
        {
            var sourceFiles = SingleSourceFor("implicit_new_expression.js", $"import UnityScript2CSharp.Tests; function F(o:Object) : {typeName} {{ F({typeName}({paramValue})); var f : {typeName} = {typeName}({paramValue}); return {typeName}({paramValue}); }}");
            var expectedConvertedContents = SingleSourceFor("implicit_new_expression.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + $"implicit_new_expression : MonoBehaviour {{ public virtual {typeName} F(object o) {{ this.F(new {typeName}({paramValue})); {typeName} f = new {typeName}({paramValue}); return new {typeName}({paramValue}); }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Implicit_New_Expression_On_Value_Type_With_No_Ctors()
        {
            var sourceFiles = SingleSourceFor("implicit_new_expression_no_params.js", "import UnityScript2CSharp.Tests; function F(o:Object) : Other { F(Other()); var f : Other = Other(); return Other(); }");
            var expectedConvertedContents = SingleSourceFor("implicit_new_expression_no_params.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + "implicit_new_expression_no_params : MonoBehaviour { public virtual Other F(object o) { this.F(default(Other)); Other f = default(Other); return default(Other); } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Implicit_Enum_To_Int_As_ValueType_Ctor_Parameter()
        {
            var sourceFiles = SingleSourceFor("implicit_enum_to_int_valuetype_param.js", "import UnityScript2CSharp.Tests; function F(s:Struct) : void {{ F(Struct(System.ConsoleColor.Black)); s = Struct(System.ConsoleColor.Black); }}");
            var expectedConvertedContents = SingleSourceFor("implicit_enum_to_int_valuetype_param.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + "implicit_enum_to_int_valuetype_param : MonoBehaviour { public virtual void F(Struct s) { this.F(new Struct((int) System.ConsoleColor.Black)); s = new Struct((int) System.ConsoleColor.Black); } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Method_Invocation()
        {
            var sourceFiles = SingleSourceFor("method_invocation.js", "function F(i:int, o:Object) { F(i, o); F(0, null); }");
            var expectedConvertedContents = SingleSourceFor("method_invocation.cs", DefaultGeneratedClass + @"method_invocation : MonoBehaviour { public virtual void F(int i, object o) { this.F(i, o); this.F(0, null); } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("var l = p", "System.Type l = p", TestName = "No false positive - infered local variable")]
        [TestCase("var l:System.Type = p", "System.Type l = p", TestName = "No false positive - local variable")]
        [TestCase("SystemTypeAsParameter.SimpleMethod(p)", "SystemTypeAsParameter.SimpleMethod(p)", TestName = "No false positive - parameter")]
        [TestCase("SystemTypeAsParameter.SimpleMethod(int)", "SystemTypeAsParameter.SimpleMethod(typeof(int))")]
        [TestCase("var o = new SystemTypeAsParameter(int)", "SystemTypeAsParameter o = new SystemTypeAsParameter(typeof(int))")]
        [TestCase("var t:System.Type = int", "System.Type t = typeof(int)")]
        public void Implicit_TypeOf_Expressions(string usSnippet, string csSnippet)
        {
            var sourceFiles = SingleSourceFor("implicit_typeof_expressions.js", $"import UnityScript2CSharp.Tests; class C {{ function F(p:System.Type) {{ {usSnippet}; }} }}");
            var expectedConvertedContents = SingleSourceFor("implicit_typeof_expressions.cs", "using UnityScript2CSharp.Tests; " + DefaultUsingsNoUnityType + $" public class C : object {{ public virtual void F(System.Type p) {{ {csSnippet}; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Implicit_TypeOf_Expressions_On_Attribute()
        {
            var sourceFiles = SingleSourceFor("implicit_typeof_expressions_on_attribute.js", "import UnityScript2CSharp.Tests; @Attr(int) class C { }");
            var expectedConvertedContents = SingleSourceFor("implicit_typeof_expressions_on_attribute.cs", "using UnityScript2CSharp.Tests; " + DefaultUsingsNoUnityType + " [UnityScript2CSharp.Tests.Attr(typeof(int))] public class C : object { }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Method_Taking_Params_System_Type()
        {
            var sourceFiles = SingleSourceFor("system_type_as_params_param.js", "import UnityScript2CSharp.Tests; function F(o:SystemTypeAsParameter) { o.InParamsArray(int, String); }");
            var expectedConvertedContents = SingleSourceFor("system_type_as_params_param.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + "system_type_as_params_param : MonoBehaviour { public virtual void F(SystemTypeAsParameter o) { o.InParamsArray(new System.Type[] {typeof(int), typeof(string)}); } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void TypeOf()
        {
            var sourceFiles = SingleSourceFor("typeof_expression.js", "function F() { return typeof(int); }");
            var expectedConvertedContents = SingleSourceFor("typeof_expression.cs", DefaultGeneratedClass + @"typeof_expression : MonoBehaviour { public virtual System.Type F() { return typeof(int); } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Indexers()
        {
            var sourceFiles = SingleSourceFor("indexers.js", "import UnityScript2CSharp.Tests; function F(p:Properties) { p[0] = 1; p[1, \"foo\"] = 2; return p[42]; }");
            var expectedConvertedContents = SingleSourceFor("indexers.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + "indexers : MonoBehaviour { public virtual int F(Properties p) { p[0] = 1; p[1, \"foo\"] = 2; return p[42]; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("\"1\"")]
        [TestCase("txt")]
        public void Indexers_With_Non_Standard_Get_Setter_Name(string expression)
        {
            var sourceFiles = SingleSourceFor("indexers2.js", $"function F(txt:String) {{ return txt.Split({expression}[0]); }}");
            var expectedConvertedContents = SingleSourceFor("indexers2.cs", DefaultGeneratedClass + $"indexers2 : MonoBehaviour {{ public virtual string[] F(string txt) {{ return txt.Split(new char[] {{{expression}[0]}}); }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("float", "double")]
        [TestCase("int", "long")]
        [TestCase("int", "float")]
        [TestCase("int", "double")]
        [TestCase("long", "double")]
        public void Cast_Is_Injected_In_Assignments_And_Arguments_If_Required(string smallType, string bigType)
        {
            var sourceFiles = SingleSourceFor("cast_upon_assignment.js", $"function F(s:{smallType}, b:{bigType}) : {smallType} {{ var s1:{smallType} = b + 1; s = b + 1; var r = F(b + 1, b); return s; }}");
            var expectedConvertedContents = SingleSourceFor("cast_upon_assignment.cs", DefaultGeneratedClass + $"cast_upon_assignment : MonoBehaviour {{ public virtual {smallType} F({smallType} s, {bigType} b) {{ {smallType} s1 = ({smallType}) (b + 1); s = ({smallType}) (b + 1); {smallType} r = this.F(({smallType}) (b + 1), b); return s; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("double", "float")]
        [TestCase("long", "int")]
        public void Cast_Is_Not_Injected_If_Not_Required(string bigType, string smallType)
        {
            var sourceFiles = SingleSourceFor("no_cast_upon_assignment.js", $"function F(s:{smallType}, b:{bigType}) : void {{ var b1:{bigType} = b + 1; b = s + 1; F(s, s + 1); }}");
            var expectedConvertedContents = SingleSourceFor("no_cast_upon_assignment.cs", DefaultGeneratedClass + $"no_cast_upon_assignment : MonoBehaviour {{ public virtual void F({smallType} s, {bigType} b) {{ {bigType} b1 = b + 1; b = s + 1; this.F(s, s + 1); }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Cast_Is_Injected_When_Assigning_Or_Passing_Object_To_Any_Other_Type()
        {
            var sourceFiles = SingleSourceFor("cast_object_to_unityengine_object.js", "import UnityScript2CSharp.Tests; function F() { var obj:Object = null; ObjectType.Parameter.<UnityEngine.Object>(obj); }");
            var expectedConvertedContents = SingleSourceFor("cast_object_to_unityengine_object.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + "cast_object_to_unityengine_object : MonoBehaviour { public virtual void F() { object obj = null; ObjectType.Parameter<UnityEngine.Object>((UnityEngine.Object) obj); } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }
    }
}
