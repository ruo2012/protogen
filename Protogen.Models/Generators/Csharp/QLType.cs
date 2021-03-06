﻿using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace Protogen.Models.Generators.Csharp
{
    class QLType
    {
        private Model _model;
        private CodeGenerator _generator = new CodeGenerator();
        public QLType(Model model)
        {
            _model = model;
        }

        public string Generate()
        {
            RenderAutogenerated();
            RenderUsingStatements();
            BeginClass();
            RenderConstructor();
            EndClass();
            return _generator.ToString();
        }

        private void RenderAutogenerated()
        {
            _generator.AppendLine("/// <auto-generated>")
                      .AppendLine("/// This file was automatically generated using Protogen.")
                      .AppendLine("/// </auto-generated>");
        }

        private void RenderUsingStatements()
        {
            _generator.AppendLine("using System;")
                      .AppendLine("using System.Linq;")
                      .AppendLine("using GraphQL;")
                      .AppendLine("using GraphQL.Types;")
                      .AppendLine($"using {_model.Project.Namespace ?? _model.Project.Name}.Models;")
                      .AppendLine();
        }

        private void BeginClass()
        {
            _generator.AppendLine($"namespace {_model.Project.Namespace ?? _model.Project.Name}.GraphQL")
                      .BeginBlock()
                      .AppendLine($"/// <summary>")
                      .AppendLine($"/// {_model.Description}")
                      .AppendLine($"/// </summary>")
                      .AppendLine($"public class {_model.Name.Pascalize()}Type : ObjectGraphType<{_model.Name.Pascalize()}>")
                      .BeginBlock();
        }

        private void EndClass()
        {
            _generator.EndBlock() //End class
                      .EndBlock(); // End namespace
        }

        private void RenderConstructor()
        {
            _generator.AppendLine($"/// <summary>")
                      .AppendLine($"/// Initializes a new instance of the <see cref=\"{_model.Name.Pascalize()}\"/> class.")
                      .AppendLine($"/// </summary>")
                      .AppendLine($"public {_model.Name.Pascalize()}Type()")
                      .BeginBlock();

            if (!_model.HasSimplePrimaryKey) RenderCompoundIdField();

            foreach (var field in _model.AllFields)
            {
                if (field.ForeignKey != null)
                {
                    RenderForeignKey(field);
                }
                else
                {
                    RenderSimpleField(field);
                }
            }

            foreach (var field in _model.ReferencingFields)
            {
                RenderInverseField(field);
            }

            _generator.EndBlock()
                      .EnsureEmptyLine();
        }

        private void RenderCompoundIdField()
        {
            _generator.AppendLine($"Field<StringGraphType>(")
                      .IncreaseIndentation()
                      .AppendLine($"\"id\",")
                      .AppendLine($"@\"\",")
                      .AppendLine($"resolve: ctx => $\"{{{string.Join("}::{", _model.PrimaryKeys.Select(f => $"ctx.Source.{f.Name.Pascalize()}"))}}}\");");
        }

        private void RenderForeignKey(ModelField field)
        {
            _generator.AppendLine($"Field<{field.ForeignKey.RefersTo.Model.Name.Pascalize()}Type>(\"{field.AccessorName.Camelize()}\", @\"{field.Description}\", resolve: ctx => ")
                      .BeginBlock()
                      .AppendLine($"var schemaContext = ({_model.Project.Name.Pascalize()}Schema.Context)ctx.UserContext;")
                      .AppendLine($"return schemaContext.Database.{field.ForeignKey.RefersTo.Model.Name.Pascalize().Pluralize()}.Where(x => x.{field.ForeignKey.RefersTo.Name.Pascalize()} == ctx.Source.{field.Name.Pascalize()}).FirstOrDefault();")
                      .EndBlock("});");
        }

        private void RenderSimpleField(ModelField field)
        {
            _generator.AppendLine($"Field(")
                      .IncreaseIndentation()
                      .AppendLine($"typeof({CsharpGenerator.Type(field.ResolvedType, false)}).GetGraphTypeFromType({field.Null.ToString().ToLower()}),")
                      .AppendLine($"\"{field.Name.Camelize()}\",")
                      .AppendLine($"@\"{field.Description}\",");

            if (field.ResolvedType.FieldType == FieldType.DateTime)
            {
                _generator.Append($"resolve: ctx => ctx.Source.{field.Name.Pascalize()}");
                if (field.Null)
                {
                    _generator.Append("?.");
                }
                else
                {
                    _generator.Append(".");
                }
                _generator.AppendLine("UtcDateTime");
            }
            else
            {
                _generator.AppendLine($"resolve: ctx => ctx.Source.{field.Name.Pascalize()}");
            }
            _generator.DecreaseIndentation()
                      .AppendLine(");");
        }

        private void RenderInverseField(ModelField field)
        {
            if (field.ResolvedInverseName != null)
            {
                _generator.AppendLine($"Connection<ListGraphType<{field.ForeignKey.RefersTo.Model.Name.Pascalize()}Type>>()")
                          .IncreaseIndentation()
                          .AppendLine($".Name(\"{field.ResolvedInverseName.Underscore()}\")")
                          .AppendLine($".Resolve(ctx => ")
                          .BeginBlock()
                          .AppendLine($"var schemaContext = ({_model.Project.Name.Pascalize()}Schema.Context)ctx.UserContext;")
                          .AppendLine($"return schemaContext.Database.{field.Model.Name.Pascalize().Pluralize()}.Where(x => x.{field.Name.Pascalize()} == ctx.Source.{field.ForeignKey.RefersTo.Name.Pascalize()});")
                          .EndBlock("});")
                          .DecreaseIndentation();
            }
        }
    }
}
