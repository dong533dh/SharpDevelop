﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using System.Text.RegularExpressions;

using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.GeneralScope
{
	[TestFixture]
	public class AttributeSectionTests
	{
		[Test]
		public void GlobalAttributeCSharp()
		{
			string program = @"[global::Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
[someprefix::DesignerGenerated()]
public class Form1 {
}";
			
			TypeDeclaration decl = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			Assert.AreEqual(2, decl.Attributes.Count);
			Assert.AreEqual("global::Microsoft.VisualBasic.CompilerServices.DesignerGenerated",
			                decl.Attributes.First().Attributes.Single().Type.ToString());
			Assert.AreEqual("someprefix::DesignerGenerated", decl.Attributes.Last().Attributes.Single().Type.ToString());
		}
		
		[Test]
		public void AssemblyAttributeCSharp()
		{
			string program = @"[assembly: System.Attribute()]";
			AttributeSection decl = ParseUtilCSharp.ParseGlobal<AttributeSection>(program);
			Assert.AreEqual(new AstLocation(1, 1), decl.StartLocation);
			Assert.AreEqual("assembly", decl.AttributeTarget);
		}
		
		[Test, Ignore("assembly/module attributes are broken")]
		public void AssemblyAttributeCSharpWithNamedArguments()
		{
			string program = @"[assembly: Foo(1, namedArg: 2, prop = 3)]";
			AttributeSection decl = ParseUtilCSharp.ParseGlobal<AttributeSection>(program);
			Assert.AreEqual("assembly", decl.AttributeTarget);
			var a = decl.Attributes.Single();
			Assert.AreEqual("Foo", a.Type);
			Assert.AreEqual(3, a.Arguments.Count());
			
			Assert.IsTrue(a.Arguments.ElementAt(0).IsMatch(new PrimitiveExpression(1)));
			Assert.IsTrue(a.Arguments.ElementAt(1).IsMatch(new NamedArgumentExpression {
			                                               	Identifier = "namedArg",
			                                               	Expression = new PrimitiveExpression(2)
			                                               }));
			Assert.IsTrue(a.Arguments.ElementAt(2).IsMatch(new AssignmentExpression {
			                                               	Left = new IdentifierExpression("prop"),
			                                               	Operator = AssignmentOperatorType.Assign,
			                                               	Right = new PrimitiveExpression(3)
			                                               }));
		}
		
		[Test, Ignore("assembly/module attributes are broken")]
		public void ModuleAttributeCSharp()
		{
			string program = @"[module: System.Attribute()]";
			AttributeSection decl = ParseUtilCSharp.ParseGlobal<AttributeSection>(program);
			Assert.AreEqual(new AstLocation(1, 1), decl.StartLocation);
			Assert.AreEqual("module", decl.AttributeTarget);
		}
		
		[Test]
		public void TypeAttributeCSharp()
		{
			string program = @"[type: System.Attribute()] class Test {}";
			TypeDeclaration type = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			AttributeSection decl = type.Attributes.Single();
			Assert.AreEqual(new AstLocation(1, 1), decl.StartLocation);
			Assert.AreEqual("type", decl.AttributeTarget);
		}
		
		[Test, Ignore("Parser doesn't support attributes on type parameters")]
		public void AttributesOnTypeParameter()
		{
			ParseUtilCSharp.AssertGlobal(
				"class Test<[A,B]C> {}",
				new TypeDeclaration {
					ClassType = ClassType.Class,
					Name = "Test",
					TypeParameters = {
						new TypeParameterDeclaration {
							Attributes = {
								new AttributeSection {
									Attributes = {
										new Attribute { Type = new SimpleType("A") },
										new Attribute { Type = new SimpleType("B") }
									}
								}
							},
							Name = "C"
						}
					}});
		}
		
		[Test]
		public void AttributeOnMethodParameter()
		{
			ParseUtilCSharp.AssertTypeMember(
				"void M([In] int p);",
				new MethodDeclaration {
					ReturnType = new PrimitiveType("void"),
					Name = "M",
					Parameters = {
						new ParameterDeclaration {
							Attributes = { new AttributeSection(new Attribute { Type = new SimpleType("In") }) },
							Type = new PrimitiveType("int"),
							Name = "p"
						}
					}});
		}
		
		[Test]
		public void AttributeOnSetterValue()
		{
			ParseUtilCSharp.AssertTypeMember(
				"int P { get; [param: In] set; }",
				new PropertyDeclaration {
					ReturnType = new PrimitiveType("int"),
					Name = "P",
					Getter = new Accessor(),
					Setter = new Accessor {
						Attributes = {
							new AttributeSection {
								AttributeTarget = "param",
								Attributes = { new Attribute { Type = new SimpleType("In") } },
							} },
					}});
		}
		
		// TODO: Tests for other contexts where attributes can appear
		
		[Test, Ignore("Parser does not support NamedArgumentExpression in attributes")]
		public void AttributeWithNamedArguments()
		{
			ParseUtilCSharp.AssertTypeMember(
				@"[A(0, a:1, b=2)] class Test {}",
				new TypeDeclaration {
					Name = "Test",
					Attributes = {
						new AttributeSection {
							Attributes = {
								new Attribute {
									Type = new SimpleType("A"),
									Arguments = {
										new PrimitiveExpression(0),
										new NamedArgumentExpression("a", new PrimitiveExpression(1)),
										new NamedExpression("b", new PrimitiveExpression(2))
									}
								}
							}
						}
					}});
		}
	}
}
