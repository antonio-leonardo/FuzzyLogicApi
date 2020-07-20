using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Web.Script.Serialization;

/// <summary>
/// Neste arquivo contém modelos e funcionalidades auxiliares
/// p/ suportar a Logica Crisp e a inferencia (saída) da Lógica Fuzzy
/// </summary>
namespace FuzzyLogicApi
{
    #region ' Inference Functionalities and Extensions '

    /// <summary>
    /// Enumerado que identifica o tipo de Resposta
    /// </summary>
    public enum ResponseType
    {
        /// <summary>
        /// Para serialização em XML
        /// </summary>
        Xml,

        /// <summary>
        /// Para serialização em JSON
        /// </summary>
        Json
    }

    /// <summary>
    /// Modelo de acesso público para expor o resultado da Inferencia
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    [Serializable, XmlRoot]
    public class InferenceResult<TEntity> where TEntity : class, new()
    {
        /// <summary>
        /// Listagem de inferencias
        /// </summary>
        [XmlElement]
        public Inference<TEntity>[] Inferences { get; set; }
    }

    /// <summary>
    /// Modelo de acesso interno para expor o resultado da Inferencia, p/ processamento em Paralelo
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    internal class IntermediateInferenceResultInternal<TEntity> where TEntity : class, new()
    {
        /// <summary>
        /// Listagem de inferencias
        /// </summary>
        internal InferenceInternal<TEntity>[] Inferences { get; set; }
    }

    /// <summary>
    /// Modelo que intermedia os resultados obtidos
    /// </summary>
    [Serializable, XmlRoot]
    public class IntermediateInferenceItem
    {
        /// <summary>
        /// As propriedades que precisam ser alteradas
        /// </summary>
        [XmlElement]
        public List<string> PropertiesNeedToChange { get; set; }

        /// <summary>
        /// Quantidade de erros baseado na coleção de validações
        /// </summary>
        [XmlElement]
        public int ErrorsQuantity { get; set; }
    }

    /// <summary>
    /// Modelo para realizar a inferencia por item, em paralelo
    /// </summary>
    internal class IntermediateInferenceItemInternal
    {
        /// <summary>
        /// Propriedades que requerem atenção para mudança
        /// </summary>
        internal List<string> PropertiesNeedToChange { get; set; }

        /// <summary>
        /// A coleção de erros ou acertos sob a lógica crisp
        /// </summary>
        internal List<bool> RatingsReport { get; set; }

        /// <summary>
        /// Indicador de erros obtidas associado a lógica crisp
        /// </summary>
        internal int ErrorsQuantity
        {
            get
            {
                int counter = 0;
                for (int i = 0; i < RatingsReport.Count; i++)
                {
                    if (!RatingsReport[i])
                    {
                        counter++;
                    }
                }
                return counter;
            }
        }
    }

    /// <summary>
    /// Modelo principal de saída do resultado da Lógica Fuzzy
    /// </summary>
    /// <typeparam name="TEntity">Classe de entidade</typeparam>
    public class Inference<TEntity> : IntermediateInferenceItem where TEntity : class, new()
    {
        /// <summary>
        /// Id do resultado
        /// </summary>
        [XmlElement]
        public string ID { get; set; }

        /// <summary>
        /// Resultado da inferencia, em decimal
        /// </summary>
        [XmlElement]
        public string InferenceResult { get; set; }

        /// <summary>
        /// Dados de entrada
        /// </summary>
        [XmlElement]
        public TEntity Data { get; set; }
    }

    /// <summary>
    /// Modelo principal de saída do resultado da Lógica Fuzzy
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    internal class InferenceInternal<TEntity> : IntermediateInferenceItemInternal where TEntity : class, new()
    {
        /// <summary>
        /// Identificador único da inferencia gerada
        /// </summary>
        internal Guid ID
        {
            get
            {
                return Guid.NewGuid();
            }
        }

        /// <summary>
        /// Resultado da inferencia
        /// </summary>
        internal string InferenceResult
        {
            get
            {
                string returning = null;
                double result = (double)RatingsReport.Where(c => c).Count() / (double)RatingsReport.Count;
                if (result == 0)
                {
                    returning = "0";
                }
                else
                {
                    returning = string.Format("{0:0.00}", result);
                }
                return returning;
            }
        }

        /// <summary>
        /// Dados de entrada, proveniente de uma classe de entidade
        /// </summary>
        internal TEntity Data { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    internal class ExpressionModel
    {
        /// <summary>
        /// 
        /// </summary>
        internal string Data { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal BinaryExpression BinaryExpressionItem { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal ExpressionModel LeftExpression { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal ExpressionModel RightExpression { get; set; }
    }

    /// <summary>
    /// Classe derivada de StringWriter
    /// p/ alteração do Encoding p/ UTF-8
    /// </summary>
    public class StringWriterUtf8 : StringWriter
    {
        public override Encoding Encoding => (new UTF8Encoding(false));
    }

    /// <summary>
    /// Classe com métodos auxiliares p/ suportar
    /// serializações e conversões de tipos
    /// </summary>
    internal static class FuzzyLogicExtensions
    {

        /// <summary>
        /// Serializa um objeto para uma string XML
        /// </summary>
        /// <typeparam name="TEntity">Tipo que será serializado</typeparam>
        /// <param name="obj">Objeto qe será submetido à Serialização</param>
        /// <returns>System.String</returns>
        internal static string XmlSerialize<TEntity>(this TEntity obj) where TEntity : class
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            using (StringWriterUtf8 writer = new StringWriterUtf8())
            {
                XmlSerializer s = new XmlSerializer(typeof(TEntity));
                s.Serialize(writer, obj, ns);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Serializa um objeto para uma string JSON
        /// </summary>
        /// <typeparam name="TEntity">Tipo que será serializado</typeparam>
        /// <param name="obj">Objeto qe será submetido à Serialização</param>
        /// <returns>System.String</returns>
        internal static string JsonSerialize<TEntity>(this TEntity obj) where TEntity : class
        {
            return (new JavaScriptSerializer()).Serialize(obj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="inferenceInternal"></param>
        /// <returns>Inference</returns>
        internal static Inference<TEntity> InternalToPublicConversion<TEntity>(this InferenceInternal<TEntity> inferenceInternal)
            where TEntity : class, new()
        {
            return new Inference<TEntity>()
            {
                Data = inferenceInternal.Data,
                ID = inferenceInternal.ID.ToString(),
                InferenceResult = inferenceInternal.InferenceResult,
                PropertiesNeedToChange = inferenceInternal.PropertiesNeedToChange,
                ErrorsQuantity = inferenceInternal.ErrorsQuantity
            };
        }
    }

    #endregion
}