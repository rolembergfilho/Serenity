using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace Serenity.CodeGenerator
{
    public class ToolsHeper
    {
        #region AcertaPalavra
        /// <summary>
        /// Acerta Palavras com a acentuacao correta
        /// </summary>
        public static string AcertaPalavra(string strPalavra)
        {
            if (strPalavra.ToUpper().IndexOf("DT_") > -1) strPalavra = strPalavra.Replace("dt_", "Dt.").Replace("nascimento", "Nasc.");
            if (strPalavra.ToUpper().IndexOf("DT. DT. ") > -1) strPalavra = strPalavra.Replace("Dt. Dt. ", "Dt.");


            if (strPalavra.ToUpper().IndexOf("DRAO") > -1) strPalavra = strPalavra.Replace("drao", "drão");
            if (strPalavra.ToUpper().IndexOf("ISICA") > -1) strPalavra = strPalavra.Replace("isica", "ísica");
            if (strPalavra.ToUpper().IndexOf("IDICA") > -1) strPalavra = strPalavra.Replace("idica", "ídica");
            if (strPalavra.ToUpper().IndexOf("CEP") > -1) strPalavra = strPalavra.Replace("Cep", "CEP");
            if (strPalavra.ToUpper().IndexOf("NF") > -1) strPalavra = strPalavra.Replace("Nf", "NF");
            if (strPalavra.ToUpper().IndexOf("CNS") > -1) strPalavra = strPalavra.Replace("Cns", "CNS");
            if (strPalavra.ToUpper().IndexOf("SUS") > -1) strPalavra = strPalavra.Replace("Sus", "SUS");
            if (strPalavra.ToUpper().IndexOf("CNAE") > -1) strPalavra = strPalavra.Replace("Cnae", "CNAE");
            if (strPalavra.ToUpper().IndexOf("TISS") > -1) strPalavra = strPalavra.Replace("Tiss", "TISS");
            if (strPalavra.ToUpper().IndexOf("CBOS") > -1) strPalavra = strPalavra.Replace("Cbos", "CBOS");
            if (strPalavra.ToUpper().IndexOf("CPF") > -1) strPalavra = strPalavra.Replace("Cpf", "CPF");
            if (strPalavra.ToUpper().IndexOf("CNPJ") > -1) strPalavra = strPalavra.Replace("Cnpj", "CNPJ");
            if (strPalavra.ToUpper().IndexOf("CARTAO") > -1) strPalavra = strPalavra.Replace("Cartao", "Cartão");
            if (strPalavra.ToUpper().IndexOf("CONJUGE") > -1) strPalavra = strPalavra.Replace("Conjuge", "Cônjuge");
            if (strPalavra.ToUpper().IndexOf("MATRICULA") > -1) strPalavra = strPalavra.Replace("tri", "trí");
            if (strPalavra.ToUpper().IndexOf("CODIGO") > -1) strPalavra = strPalavra.Replace("odigo", "ódigo");
            if (strPalavra.ToUpper().IndexOf("IVEL") > -1) strPalavra = strPalavra.Replace("ivel", "ível");
            if (strPalavra.ToUpper().IndexOf("TANCIA") > -1) strPalavra = strPalavra.Replace("tancia", "tância");
            if (strPalavra.ToUpper().IndexOf("EDIA") > -1) strPalavra = strPalavra.Replace("edia", "édia");
            if (strPalavra.ToUpper().IndexOf("ARIO") > -1) strPalavra = strPalavra.Replace("ario", "ário");
            if (strPalavra.ToUpper().IndexOf("ALISE") > -1) strPalavra = strPalavra.Replace("alise", "álise");
            if (strPalavra.ToUpper().IndexOf("DIARIA") > -1) strPalavra = strPalavra.Replace("aria", "ária");
            if (strPalavra.ToUpper().IndexOf("SAIDA") > -1) strPalavra = strPalavra.Replace("ida", "ída");
            if (strPalavra.ToUpper().IndexOf("DISTURBIO") > -1) strPalavra = strPalavra.Replace("Disturbio", "Distúrbio");
            if (strPalavra.ToUpper().IndexOf("MEDICO") > -1) strPalavra = strPalavra.Replace("Medico", "Médico");
            if (strPalavra.ToUpper().IndexOf("CONVENIO") > -1) strPalavra = strPalavra.Replace("enio", "ênio");
            if (strPalavra.ToUpper().IndexOf("AVEL") > -1) strPalavra = strPalavra.Replace("avel", "ável");
            //if (strPalavra.ToUpper().IndexOf("SSAO") > -1) strPalavra = strPalavra.Replace("ssao", "ssão");
            if (strPalavra.ToUpper().IndexOf("SAO") > -1) strPalavra = strPalavra.Replace("sao", "são");
            if (strPalavra.ToUpper().IndexOf("NM_") > -1) strPalavra = strPalavra.Replace("nm_", "Nome");
            if (strPalavra.ToUpper().IndexOf("DS_") > -1) strPalavra = strPalavra.Replace("ds_", "Desc.");
            if (strPalavra.ToUpper().IndexOf("VL_") > -1) strPalavra = strPalavra.Replace("vl_", "Vl.");
            if (strPalavra.ToUpper().IndexOf("TEL") > -1) strPalavra = strPalavra.Replace("Telefone", "Tel.");
            if (strPalavra.ToUpper().IndexOf("ENDERECO") > -1) strPalavra = strPalavra.Replace("Endereco", "Endereço");
            if (strPalavra.ToUpper().IndexOf("INCLUSAO") > -1) strPalavra = strPalavra.Replace("Inclusao", "Inclusão");
            if (strPalavra.ToUpper().IndexOf("INICIO") > -1) strPalavra = strPalavra.Replace("Inicio", "Início");
            if (strPalavra.ToUpper().IndexOf("CAO") > -1) strPalavra = strPalavra.Replace("cao", "ção");
            if (strPalavra.ToUpper().IndexOf("COES") > -1) strPalavra = strPalavra.Replace("coes", "ções");
            if (strPalavra.ToUpper().IndexOf("ESPECIE") > -1) strPalavra = strPalavra.Replace("Especie", "Espécie");
            if (strPalavra.ToUpper().IndexOf("VEICULO") > -1) strPalavra = strPalavra.Replace("Veiculo", "Veículo");
            if (strPalavra.ToUpper().IndexOf("TIVEL") > -1) strPalavra = strPalavra.Replace("tivel", "tível");
            if (strPalavra.ToUpper().IndexOf("ZAO") > -1) strPalavra = strPalavra.Replace("zao", "zão");
            if (strPalavra.ToUpper().IndexOf("DT. DATA") > -1) strPalavra = strPalavra.Replace("Dt. Data", "Dt.");
            if (strPalavra.ToUpper().IndexOf("ZOES") > -1) strPalavra = strPalavra.Replace("zoes", "zões");
            if (strPalavra.ToUpper().IndexOf("E-MAIL") > -1) strPalavra = strPalavra.Replace("E-Mail", "E-mail");
            if (strPalavra.ToUpper().IndexOf("EMAIL") > -1) strPalavra = strPalavra.Replace("Email", "E-mail");
            if (strPalavra.ToUpper().IndexOf("COMENTARIO") > -1) strPalavra = strPalavra.Replace("Comentario", "Comentário");
            if (strPalavra.ToUpper().IndexOf("NUMERO") > -1) strPalavra = strPalavra.Replace("Numero", "Número");
            if (strPalavra.ToUpper().IndexOf("MAE") > -1) strPalavra = strPalavra.Replace("ae", "ãe");
            if (strPalavra.ToUpper().IndexOf("ORGAO") > -1) strPalavra = strPalavra.Replace("Orgao", "Órgão");
            if (strPalavra.ToUpper().IndexOf("HISTORICO") > -1) strPalavra = strPalavra.Replace("Historico", "Histórico");
            if (strPalavra.ToUpper().IndexOf("PERIODO") > -1) strPalavra = strPalavra.Replace("Periodo", "Período");
            if (strPalavra.ToUpper().IndexOf("BANCARIO") > -1) strPalavra = strPalavra.Replace("Bancario", "Bancário");
            if (strPalavra.ToUpper().IndexOf("SERIE") > -1) strPalavra = strPalavra.Replace("Serie", "Série");
            if (strPalavra.ToUpper().IndexOf("TITULO") > -1) strPalavra = strPalavra.Replace("itulo", "ítulo");
            if (strPalavra.ToUpper().IndexOf("DESC. DESCRIÇÃO") > -1) strPalavra = strPalavra.Replace("DESCRIÇÃO ", "").Replace("descrição ", "").Replace("Descrição ", "");
            if (strPalavra.ToUpper().IndexOf("ULTIM") > -1) strPalavra = strPalavra.Replace("Ultim", "Últim");
            if (strPalavra.ToUpper().IndexOf("TIPO") > -1) strPalavra = strPalavra.Replace("Tipo", "Tipo").Replace("  ", "");
            if (strPalavra.ToUpper().IndexOf("ENCIA") > -1) strPalavra = strPalavra.Replace("encia", "ência");


            return strPalavra;
        }
        #endregion
    }
}
