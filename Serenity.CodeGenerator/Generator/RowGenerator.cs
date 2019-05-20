using Serenity.Data;
using Serenity.Data.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Serenity.CodeGenerator
{
    public class RowGenerator
    {
        private static int DeterminePrefixLength<T>(IEnumerable<T> list, Func<T, string> getName)
        {
            if (!Enumerable.Any<T>(list))
                return 0;
            string str1 = getName(Enumerable.First<T>(list));
            int length = str1.IndexOf('_');
            if (length <= 0)
                return 0;
            string str2 = str1.Substring(0, length + 1);
            foreach (T obj in list)
            {
                if (!getName(obj).StartsWith(str2) || getName(obj).Length == str2.Length)
                    return 0;
            }
            return str2.Length;
        }

        public static string JI(string join, string field)
        {
            if (field.ToLowerInvariant() == join.ToLowerInvariant())
                return field;
            else
                return join + field;
        }

        public static string JU(string join, string field)
        {
            if (join.ToLowerInvariant() == field.ToLowerInvariant())
                return field;
            else
                return join + "_" + field;
        }

        public static string FieldTypeToTS(string ft)
        {
            switch (ft)
            {
                case "Boolean":
                    return "boolean";
                case "String":
                case "DateTime":
                case "TimeSpan":
                case "Guid":
                    return "string";
                case "Int32":
                case "Int16":
                case "Int64":
                case "Single":
                case "Double":
                case "Decimal":
                    return "number";
                case "Stream":
                case "ByteArray":
                    return "number[]";
            }

            return "any";
        }

        private static EntityField ToEntityField(FieldInfo fieldInfo, int prefixLength, GeneratorConfig config)
        {
            string flags;
            if (fieldInfo.IsIdentity)
                flags = "Identity";
            else if (fieldInfo.IsPrimaryKey)
                flags = "PrimaryKey";
            else if (fieldInfo.DataType == "timestamp" || fieldInfo.DataType == "rowversion")
                flags = "Insertable(false), Updatable(false), NotNull";
            else if (!fieldInfo.IsNullable)
                flags = "NotNull";
            else
                flags = null;

            string dataType;
            var fieldType = SchemaHelper.SqlTypeNameToFieldType(fieldInfo.DataType, fieldInfo.Size, out dataType);
            dataType = dataType ?? fieldType;
            return new EntityField
            {
                FieldType = fieldType,
                DataType = dataType,
                IsValueType = fieldType != "String" && fieldType != "Stream" && fieldType != "ByteArray",
                TSType = FieldTypeToTS(fieldType),
                Ident = GenerateVariableName(fieldInfo.FieldName.Substring(prefixLength)),

                //ROLEMBERG FILHO - trata o Tilte de acordo com as regras de AcertaPalavras
                //Title = Inflector.Inflector.Titleize(fieldInfo.FieldName.Substring(prefixLength)),
                Title = config.ReplaceStringinDisplayName ? ToolsHeper.AcertaPalavra(Inflector.Inflector.Titleize(fieldInfo.FieldName.Substring(prefixLength))) :
                                     Inflector.Inflector.Titleize(fieldInfo.FieldName.Substring(prefixLength)),
                //ROLEMBERG FILHO - trata o Tilte de acordo com as regras de AcertaPalavras


                Flags = flags,
                Name = fieldInfo.FieldName,
                Size = fieldInfo.Size == 0 ? (Int32?)null : fieldInfo.Size,
                Scale = fieldInfo.Scale
            };
        }

        public static EntityModel GenerateModel(IDbConnection connection, string tableSchema, string table,
            string module, string connectionKey, string entityClass, string permission, GeneratorConfig config)
        {
            var model = new EntityModel();
            model.Module = module;

            if (connection.GetDialect().ServerType.StartsWith("MySql", StringComparison.OrdinalIgnoreCase))
                model.Schema = null;
            else
                model.Schema = tableSchema;

            model.Permission = permission;
            model.ConnectionKey = connectionKey;
            model.RootNamespace = config.RootNamespace;
            var className = entityClass ?? ClassNameFromTableName(table);
            model.ClassName = className;
            model.RowClassName = className + "Row";
            model.Title = Inflector.Inflector.Titleize(className);
            model.Tablename = table;
            model.Fields = new List<EntityField>();
            model.Joins = new List<EntityJoin>();
            model.Instance = true;
            model.DialogAttributes = new DialogAttributes();                   /*ROLEMBERG FILHO*/
            model.DialogAttributes.Attrs01 = new List<string>();               /*ROLEMBERG FILHO*/
            model.DialogAttributes.Attrs02 = new List<string>();               /*ROLEMBERG FILHO*/
            model.DialogAttributes.Attrs03 = new List<string>();               /*ROLEMBERG FILHO*/
            model.DialogAttributes.AttrsConstructor = new List<string>();      /*ROLEMBERG FILHO*/
            model.DialogAttributes.AttrsValidacao = new List<string>();        /*ROLEMBERG FILHO*/
            //model.DialogAttributes.AttrsConfirmSave = new List<string>();      /*ROLEMBERG FILHO*/
            //model.DialogAttributes.Attrs03 = processAdvancedTips_Model(ref model); /*ROLEMBERG FILHO*/
            processAdvancedTips_Model(ref model); /*ROLEMBERG FILHO*/

            var schemaProvider = SchemaHelper.GetSchemaProvider(connection.GetDialect().ServerType);
            var fields = schemaProvider.GetFieldInfos(connection, tableSchema, table).ToList();
            if (!fields.Any(x => x.IsPrimaryKey))
            {
                var primaryKeys = new HashSet<string>(schemaProvider.GetPrimaryKeyFields(connection, tableSchema, table));
                foreach (var field in fields)
                    field.IsPrimaryKey = primaryKeys.Contains(field.FieldName);
            }

            if (!fields.Any(x => x.IsIdentity))
            {
                var identities = new HashSet<string>(schemaProvider.GetIdentityFields(connection, tableSchema, table));
                foreach (var field in fields)
                    field.IsIdentity = identities.Contains(field.FieldName);
            }

            var foreigns = schemaProvider.GetForeignKeys(connection, tableSchema, table)
                .ToLookup(x => x.FKName)
                .Where(x => x.Count() == 1)
                .SelectMany(x => x)
                .ToList();

            foreach (var field in fields)
            {
                var fk = foreigns.FirstOrDefault(x => x.FKColumn == field.FieldName);
                if (fk != null)
                {
                    field.PKSchema = fk.PKSchema;
                    field.PKTable = fk.PKTable;
                    field.PKColumn = fk.PKColumn;
                }
            }

            var prefix = DeterminePrefixLength(fields, x => x.FieldName);

            model.FieldPrefix = fields.First().FieldName.Substring(0, prefix);

            var identity = fields.FirstOrDefault(f => f.IsIdentity == true);
            if (identity == null)
                identity = fields.FirstOrDefault(f => f.IsPrimaryKey == true);
            if (identity != null)
                model.Identity = GenerateVariableName(identity.FieldName.Substring(prefix));
            else
            {
                identity = fields.FirstOrDefault(f => f.IsPrimaryKey == true) ??
                    fields.FirstOrDefault();
                if (identity != null)
                    model.Identity = GenerateVariableName(identity.FieldName.Substring(prefix));
            }

            string baseRowMatch = null;
            HashSet<string> baseRowFieldset = null;
            List<string> baseRowFieldList = new List<string>();
            foreach (var k in config.BaseRowClasses ?? new List<GeneratorConfig.BaseRowClass>())
            {
                var b = k.ClassName;
                var f = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var fl = new List<string>();
                bool skip = false;
                foreach (var s in k.Fields ?? new List<string>())
                {
                    string n = s.TrimToNull();
                    if (n == null || !fields.Any(z => z.FieldName.Substring(prefix) == n))
                    {
                        skip = true;
                        break;
                    }
                    f.Add(n);
                    fl.Add(n);
                }

                if (skip)
                    continue;

                if (baseRowFieldset == null || f.Count > baseRowFieldset.Count)
                {
                    baseRowFieldset = f;
                    baseRowFieldList = fl;
                    baseRowMatch = b;
                }
            }

            var removeForeignFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in config.RemoveForeignFields ?? new List<string>())
            {
                string n = s.TrimToNull();
                if (n != null)
                    removeForeignFields.Add(n);
            }

            if (baseRowFieldset != null &&
                baseRowFieldset.Count > 0)
            {
                model.RowBaseClass = baseRowMatch;
                model.FieldsBaseClass = baseRowMatch + "Fields";
                model.RowBaseFields = new List<EntityField>();
                fields = fields.Where(f =>
                {
                    if (baseRowFieldset.Contains(f.FieldName.Substring(prefix)))
                    {
                        var ef = ToEntityField(f, prefix, config);
                        ef.Flags = null;
                        model.RowBaseFields.Add(ef);
                        return false;
                    }
                    return true;
                }).ToList();
            }
            else
            {
                model.RowBaseClass = "Row";
                model.RowBaseFields = new List<EntityField>();
                model.FieldsBaseClass = "RowFieldsBase";
            }

            var fieldByIdent = new Dictionary<string, EntityField>(StringComparer.OrdinalIgnoreCase);

            foreach (var field in fields)
            {
                var f = ToEntityField(field, prefix, config);

                if (f.Ident == model.IdField)
                    f.ColAttributes = "EditLink, DisplayName(\"Db.Shared.RecordId\"), AlignRight";

                int i = 0;
                string ident = f.Ident;
                while (fieldByIdent.ContainsKey(ident))
                    ident = f.Ident + ++i;
                f.Ident = ident;
                fieldByIdent[ident] = f;

                if (f.Name == className && f.FieldType == "String")
                {
                    model.NameField = f.Name;
                    f.ColAttributes = f.ColAttributes ?? "EditLink";
                }

                //ROLEMBERG FILHO - processa advanced tips para COLUNAS
                f.ColAttributes += processAdvancedTips_Columns(f);
                //ROLEMBERG FILHO - processa advanced tips para COLUNAS

                var foreign = foreigns.Find((k) => k.FKColumn.Equals(field.FieldName, StringComparison.OrdinalIgnoreCase));
                if (foreign != null)
                {
                    if (f.Title.EndsWith(" Id") && f.Title.Length > 3)
                        f.Title = f.Title.SafeSubstring(0, f.Title.Length - 3);

                    f.PKSchema = foreign.PKSchema;
                    f.PKTable = foreign.PKTable;
                    f.PKColumn = foreign.PKColumn;

                    var frgfld = schemaProvider.GetFieldInfos(connection, foreign.PKSchema, foreign.PKTable).ToList();
                    int frgPrefix = RowGenerator.DeterminePrefixLength(frgfld, z => z.FieldName);
                    var j = new EntityJoin();
                    j.Fields = new List<EntityField>();
                    j.Name = GenerateVariableName(f.Name.Substring(prefix));
                    if (j.Name.EndsWith("Id") || j.Name.EndsWith("ID"))
                        j.Name = j.Name.Substring(0, j.Name.Length - 2);
                    f.ForeignJoinAlias = j.Name;
                    j.SourceField = f.Ident;

                    frgfld = frgfld.Where(y => !removeForeignFields.Contains(y.FieldName)).ToList();

                    foreach (var frg in frgfld)
                    {
                        if (frg.FieldName.Equals(foreign.PKColumn, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var k = ToEntityField(frg, frgPrefix, config);
                        k.Flags = null;

                        //ROLEMBERG FILHO - trata o Tilte de acordo com as regras de AcertaPalavras
                        //k.Title = Inflector.Inflector.Titleize(JU(j.Name, frg.FieldName.Substring(frgPrefix)));
                        k.Title = config.ReplaceStringinDisplayName ? ToolsHeper.AcertaPalavra(Inflector.Inflector.Titleize(JU(j.Name, frg.FieldName.Substring(frgPrefix)))) :
                                     Inflector.Inflector.Titleize(JU(j.Name, frg.FieldName.Substring(frgPrefix)));
                        //ROLEMBERG FILHO - trata o Tilte de acordo com as regras de AcertaPalavras

                        k.Ident = JI(j.Name, k.Ident);
                        i = 0;
                        ident = k.Ident;
                        while (fieldByIdent.ContainsKey(ident))
                            ident = k.Ident + ++i;
                        k.Ident = ident;
                        fieldByIdent[ident] = k;

                        var atk = new List<string>();
                        atk.Add("DisplayName(\"" + k.Title + "\")");
                        k.Expression = "j" + j.Name + ".[" + k.Name + "]";
                        atk.Add("Expression(\"" + k.Expression + "\")");
                        k.Attributes = String.Join(", ", atk);

                        if (f.TextualField == null && k.FieldType == "String")
                            f.TextualField = k.Ident;

                        j.Fields.Add(k);
                    }

                    model.Joins.Add(j);
                }

                model.Fields.Add(f);
            }

            if (model.NameField == null)
            {
                var fld = model.Fields.FirstOrDefault(z => z.FieldType == "String");
                if (fld != null)
                {
                    model.NameField = fld.Ident;
                    fld.ColAttributes = fld.ColAttributes ?? "EditLink";
                }
            }

            foreach (var x in model.Fields)
            {
                var attrs = new List<string>();
                //ROLEMBERG FILHO - lookup Editor Form
                var attrsLookupEditorForm = new List<string>();
                //ROLEMBERG FILHO - lookup Editor Form

                attrs.Add("DisplayName(\"" + x.Title + "\")");

                if (x.Ident != x.Name)
                    attrs.Add("Column(\"" + x.Name + "\")");

                if ((x.Size ?? 0) > 0)
                    attrs.Add("Size(" + x.Size + ")");

                if (x.Scale > 0)
                    attrs.Add("Scale(" + x.Scale + ")");

                if (!String.IsNullOrEmpty(x.Flags))
                    attrs.Add(x.Flags);

                if (!String.IsNullOrEmpty(x.PKTable))
                {
                    attrs.Add("ForeignKey(\"" + (string.IsNullOrEmpty(x.PKSchema) ? x.PKTable : ("[" + x.PKSchema + "].[" + x.PKTable + "]")) + "\", \"" + x.PKColumn + "\")");
                    attrs.Add("LeftJoin(\"j" + x.ForeignJoinAlias + "\")");

                    //ROLEMBERG FILHO - trata o LOOKUPEDITOR
                    attrsLookupEditorForm.Add("LookupEditor(typeof(" + model.Module + ".Entities." + Serenity.CodeGenerator.RowGenerator.ClassNameFromTableName(x.PKTable) + "Row), InplaceAdd = true)");
                    //ROLEMBERG FILHO - trata o LOOKUPEDITOR

                }

                if (model.NameField == x.Ident)
                    attrs.Add("QuickSearch");

                if (x.TextualField != null)
                    attrs.Add("TextualField(\"" + x.TextualField + "\")");

                //ROLEMBERG FILHO - trata o PLACEHOLDER e ADVANCED TIPS
                //if (config.FieldDescriptionasPlaceholder)
                //{
                if (!string.IsNullOrEmpty(x.FieldDescription))
                    if (x.DataType == "Boolean")
                        attrs.Add("Hint(\"" + x.FieldDescription + "\")");
                    else
                        attrs.Add("Placeholder(\"" + x.FieldDescription + "\")");
                //}

                //if (config.GenerateRowswithAdvancedTips)
                //{
                string attr = processAdvancedTips(x);

                if (!string.IsNullOrEmpty(attr))
                    attrs.Add(attr);
                //}

                //ROLEMBERG FILHO - trata o PLACEHOLDER e ADVANCED TIPS

                x.Attributes = String.Join(", ", attrs.ToArray());
                x.AttrsLookupEditorForm = String.Join(", ", attrsLookupEditorForm.ToArray());
                x.AttrsFileUpload = processAdvancedTips_Image_File(x, model.Tablename);
                x.FormAttributes = processAdvancedTips_Forms(x);
                //x.DialogAttributes = processAdvancedTips_Dialog(x, model.RootNamespace);
                processAdvancedTips_Dialog(x, ref model);
            }

            return model;
        }

        private static bool IsStringLowerCase(string s)
        {
            foreach (char c in s)
                if (!Char.IsLower(c))
                    return false;
            return s.Length > 0;
        }

        public static string GenerateVariableName(string fieldName)
        {
            return Inflector.Inflector.Titleize(fieldName).Replace(" ", "");
        }

        public static string ClassNameFromTableName(string tableName)
        {
            tableName = tableName.Replace(" ", "");
            if (tableName.StartsWith("tb_"))
                tableName = tableName.Substring(3);
            else if (tableName.StartsWith("aspnet_"))
                tableName = "AspNet" + tableName.Substring(7);
            return RowGenerator.GenerateVariableName(tableName);
        }

        private static string ClassNameToLowerCase(string className)
        {
            className = StringHelper.TrimToNull(className);
            if (className == null)
                return className;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < className.Length; i++)
            {
                char c = className[i];
                if (Char.IsUpper(c) &&
                    c >= 'A' &&
                    c <= 'Z')
                {
                    c = Char.ToLowerInvariant(c);
                    if (i > 0 &&
                        !Char.IsUpper(className[i - 1]) &&
                        className[i - 1] != '_')
                        sb.Append("_");
                    sb.Append(c);
                }
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        //ROLEMBERG - process Advanced Tips
        private static string processAdvancedTips(Serenity.CodeGenerator.EntityField x)
        {
            string DateTimeField = "";
            if (x.DataType == "Date")
            {
                DateTimeField = "DateEditor";
            }

            if (x.DataType == "Time")
            {
                DateTimeField = "TimeEditor";
            }

            if (x.DataType == "DateTime")
            {
                DateTimeField = "DateTimeEditor";
            }

            if ((x.DataType == "DateTime") || (x.DataType == "Date"))
            {
                if ((x.Ident.ToUpper().Contains("CADASTRO")) || (x.Ident.ToUpper().Contains("INSERT")))
                {
                    DateTimeField = "ReadOnly(true), DefaultValue(\"now\"), Updatable(false), " + DateTimeField;
                }

                if ((x.Ident.ToUpper().Contains("ATUALIZACAO")) || (x.Ident.ToUpper().Contains("UPDATE")))
                {
                    DateTimeField = "ReadOnly(true), " + DateTimeField;
                }
            }

            if (!string.IsNullOrEmpty(DateTimeField))
                return DateTimeField;

            if ((x.DataType == "String") && ((x.Size ?? 0) >= 200) &&
                ((!x.Ident.ToUpper().Contains("ARQUIVO")) || (!x.Ident.ToUpper().Contains("FOTO"))))
            {
                return "TextAreaEditor(Rows = 4)";
            }
            return "";
        }

        private static string processAdvancedTips_Image_File(Serenity.CodeGenerator.EntityField x, string tableName)
        {
            if ((x.DataType == "String") && (x.Ident.ToUpper().Contains("FOTO")))
            {
                string strMultiple = "";
                if (x.Ident.ToUpper().Contains("FOTOS"))
                    strMultiple = "Multiple";
                return "[" + strMultiple + "ImageUploadEditor(AllowNonImage = true, FilenameFormat = \"" + tableName + "/" + x.Ident + "/~\")]";
            }

            if ((x.DataType == "String") && (x.Ident.ToUpper().Contains("ARQUIVO")))
            {
                string strMultiple = "";
                if (x.Ident.ToUpper().Contains("ARQUIVOS"))
                    strMultiple = "Multiple";
                return "[" + strMultiple + "FileUploadEditor(AllowNonImage = true, FilenameFormat = \"" + tableName + "/" + x.Ident + "/~\")]    //, OriginalNameProperty = \"" + x.Ident + "Nome\")]";
            }
            return "";
        }

        private static string processAdvancedTips_Columns(Serenity.CodeGenerator.EntityField x)
        {
            if (x.DataType == "Decimal")
                return "DisplayFormat(\"#,##0.00\"), AlignRight";
            return "";
        }

        private static string processAdvancedTips_Forms(Serenity.CodeGenerator.EntityField x)
        {
            //if (x.DataType == "Decimal")
            //    return "DisplayFormat(\"#,##0.00\"), AlignRight";

            if ((x.DataType == "String") && (x.Ident.ToUpper().Contains("MAIL")))
            {
                return "EmailEditor";
            }
            if ((x.DataType == "String") && (x.Ident.ToUpper().Contains("TELEFONE")))
            {
                return "MaskedEditor(Mask = \"(99)9999-9999\")";
            }
            if ((x.DataType == "String") && (x.Ident.ToUpper().Contains("CPF")))
            {
                return "MaskedEditor(Mask = \"999.999.999-99\")";
            }
            if ((x.DataType == "String") && (x.Ident.ToUpper().Contains("CNPJ")))
            {
                return "MaskedEditor(Mask = \"99.999.999/9999-99\")";
            }
            return "";
        }

        private static DialogAttributes processAdvancedTips_Dialog(Serenity.CodeGenerator.EntityField x, ref EntityModel m)
        {
            //DialogAttributes m.DialogAttributes = new DialogAttributes();
            //m.DialogAttributes.Attrs01 = new List<string>();
            //m.DialogAttributes.Attrs02 = new List<string>();
            //m.DialogAttributes.AttrsConstructor = new List<string>();
            //m.DialogAttributes.AttrsValidacao = new List<string>();
            //m.DialogAttributes.AttrsConfirmSave = new List<string>();

            #region // *** DIALOG PARA CONFIRMAR SE VAI SALVAR OU NÃO ***
            //m.DialogAttributes.AttrsConfirmSave.Add("            // *** DIALOG PARA CONFIRMAR SE VAI SALVAR OU NÃO ***");
            //m.DialogAttributes.AttrsConfirmSave.Add("            " + (m.RootNamespace) + "DialogUtils.pendingChangesConfirmation(this.element, () => this.getSaveState() != this.loadedState);");
            //m.DialogAttributes.AttrsConfirmSave.Add("        }");
            //m.DialogAttributes.AttrsConfirmSave.Add("");
            //m.DialogAttributes.AttrsConfirmSave.Add("//INICIO - PROCESSA O CONFIRM SAVE");
            //m.DialogAttributes.AttrsConfirmSave.Add("        getSaveState() {");
            //m.DialogAttributes.AttrsConfirmSave.Add("            try { return $.toJSON(this.getSaveEntity()); }");
            //m.DialogAttributes.AttrsConfirmSave.Add("            catch (e) { return null; }");
            //m.DialogAttributes.AttrsConfirmSave.Add("        }");
            //m.DialogAttributes.AttrsConfirmSave.Add("");
            //m.DialogAttributes.AttrsConfirmSave.Add("        loadResponse(data) {");
            //m.DialogAttributes.AttrsConfirmSave.Add("            super.loadResponse(data);");
            //m.DialogAttributes.AttrsConfirmSave.Add("            this.loadedState = this.getSaveState();");
            //m.DialogAttributes.AttrsConfirmSave.Add("        }");
            //m.DialogAttributes.AttrsConfirmSave.Add("//FIM - PROCESSA O CONFIRM SAVE");
            #endregion

            #region MyRegion
            if ((x.DataType == "DateTime") &&
            ((x.Ident.ToUpper().Contains("ATUALIZACAO")) || (x.Ident.ToUpper().Contains("UPDATE"))))
            {
                m.DialogAttributes.Attrs01.Add("this.form." + x.Ident + ".valueAsDate = new Date();");
            }

            if ((x.DataType == "Boolean") && ((x.Ident.ToUpper().Contains("ATIVO"))))
            {
                m.DialogAttributes.Attrs02.Add("//if (this.form." + x.Ident + ".value == true)");
                m.DialogAttributes.Attrs02.Add("//{");
                m.DialogAttributes.Attrs02.Add("//    this.form.ALGUMCAMPO.getGridField().toggle(true);");
                m.DialogAttributes.Attrs02.Add("//}");
            }

            if ((x.DataType == "Boolean") && ((x.Ident.ToUpper().Contains("ATIVO"))))
            {
                m.DialogAttributes.AttrsConstructor.Add("//// *** INICIO - CHECKBOX CHANGE - " + x.Ident + " ***");
                m.DialogAttributes.AttrsConstructor.Add("//this.form." + x.Ident + ".change(e => {");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//    if (this.form." + x.Ident + ".value == true) {");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//        var isChecked = false;");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//        Q.confirm(\"Confirma a seleção ?\", ");
                m.DialogAttributes.AttrsConstructor.Add("//            () => {");
                m.DialogAttributes.AttrsConstructor.Add("//                isChecked = true;");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//                //var texto = this.form.ALGUMCAMPO.getGridField().find('.caption').prop('outerHTML').split('Nome').join('TEXTO NOVO');");
                m.DialogAttributes.AttrsConstructor.Add("//                //this.form.ALGUMCAMPO.getGridField().find('.caption').prop('outerHTML', texto);");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//                this.form." + x.Ident + ".value = isChecked;");
                m.DialogAttributes.AttrsConstructor.Add("//                this.form.ALGUMCAMPO.getGridField().toggle(true);");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//                this.form.ALGUMCAMPO.value = null;");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//            }");
                m.DialogAttributes.AttrsConstructor.Add("//        );");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//        this.form." + x.Ident + ".value = isChecked;");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//      }");
                m.DialogAttributes.AttrsConstructor.Add("//      else {");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//          var isChecked = true;");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//          Q.confirm(\"Confirma a exclusão ?\\nOs dados existentes serão descartados.\", ");
                m.DialogAttributes.AttrsConstructor.Add("//              () => {");
                m.DialogAttributes.AttrsConstructor.Add("//              isChecked = false;");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//                  this.form." + x.Ident + ".value = isChecked;");
                m.DialogAttributes.AttrsConstructor.Add("//                  this.form.ALGUMCAMPO.getGridField().toggle(false);");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//            }");
                m.DialogAttributes.AttrsConstructor.Add("//        );");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//        this.form." + x.Ident + ".value = isChecked;");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("//    }");
                m.DialogAttributes.AttrsConstructor.Add("//});");
                m.DialogAttributes.AttrsConstructor.Add("//// *** FIM - CHECKBOX CHANGE - NotaFiscalTerceiro ***");
                m.DialogAttributes.AttrsConstructor.Add("//");
                m.DialogAttributes.AttrsConstructor.Add("");
                m.DialogAttributes.AttrsConstructor.Add("");
            }

            if ((x.DataType == "String") && ((x.Ident.ToUpper().Contains("CNPJ"))))
                m.DialogAttributes.AttrsValidacao.Add(m.RootNamespace + ".addValidationRule_CNPJ(this.form." + x.Ident + ");");

            if ((x.DataType == "String") && ((x.Ident.ToUpper().Contains("CPF"))))
                m.DialogAttributes.AttrsValidacao.Add(m.RootNamespace + ".addValidationRule_CPF(this.form." + x.Ident + ");");
            #endregion

            return m.DialogAttributes;
        }

        private static void processAdvancedTips_Model(ref Serenity.CodeGenerator.EntityModel m)
        {

            switch (m.ClassName.ToUpper())
            {
                case "PACIENTES":
                case "CONVENIOS":
                case "PROFISSIONAIS":
                case "FORNECEDOR":
                    var attr = new List<string>();
                    m.DialogAttributes.Attrs03.Add("			//// passa o nome do form do " + m.ClassName + " (controle interno de CONTATOS )");
                    m.DialogAttributes.Attrs03.Add("			//this.form.ContatosList.myParentForm = " + m.ClassName + ";");
                    break;

                default:
                    break;
            }

            #region // *** DIALOG PARA CONFIRMAR SE VAI SALVAR OU NÃO ***
            m.DialogAttributes.AttrsConfirmSave.Add("    // *** DIALOG PARA CONFIRMAR SE VAI SALVAR OU NÃO ***");
            m.DialogAttributes.AttrsConfirmSave.Add("            " + (m.RootNamespace) + ".DialogUtils.pendingChangesConfirmation(this.element, () => this.getSaveState() != this.loadedState);");
            m.DialogAttributes.AttrsConfirmSave.Add("        }");
            m.DialogAttributes.AttrsConfirmSave.Add("");
            m.DialogAttributes.AttrsConfirmSave.Add("        //INICIO - PROCESSA O CONFIRM SAVE");
            m.DialogAttributes.AttrsConfirmSave.Add("        getSaveState() {");
            m.DialogAttributes.AttrsConfirmSave.Add("            try { return $.toJSON(this.getSaveEntity()); }");
            m.DialogAttributes.AttrsConfirmSave.Add("            catch (e) { return null; }");
            m.DialogAttributes.AttrsConfirmSave.Add("        }");
            m.DialogAttributes.AttrsConfirmSave.Add("");
            m.DialogAttributes.AttrsConfirmSave.Add("        loadResponse(data) {");
            m.DialogAttributes.AttrsConfirmSave.Add("            super.loadResponse(data);");
            m.DialogAttributes.AttrsConfirmSave.Add("            this.loadedState = this.getSaveState();");
            m.DialogAttributes.AttrsConfirmSave.Add("        }");
            m.DialogAttributes.AttrsConfirmSave.Add("        //FIM - PROCESSA O CONFIRM SAVE");
            #endregion

        }


    }
}