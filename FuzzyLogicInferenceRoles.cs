using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace FuzzyLogicApi
{
    #region ' Inference Functionalities and Extensions '
    /// <summary>
    /// 
    /// </summary>
    public enum ResponseType
    {
        /// <summary>
        /// 
        /// </summary>
        Xml,

        ///
        Json
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    [Serializable, XmlRoot]
    public class InferenceResult<TEntity> where TEntity : class, new()
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlElement]
        public Inferecence<TEntity>[] Inferences { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class IntermediateInferenceItem
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlElement]
        public List<string> PropertiesNeedToChange { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElement]
        public List<bool> RatingsReport { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElement]
        public int ErrorsQuantity { get { return RatingsReport.Where(c => !c).Count(); } }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    [Serializable, XmlRoot]
    public class Inferecence<TEntity> : IntermediateInferenceItem where TEntity : class, new()
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlElement]
        public int ID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElement]
        public string HitsPercentage
        {
            get
            {
                int percentage = (int)(((double)RatingsReport.Where(c => c).Count() / (double)RatingsReport.Count) * 100);
                return percentage.ToString() + "%";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [XmlElement]
        public TEntity Data { get; set; }
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

    internal static class FuzzyLogicExtensions
    {
        /// <summary>
        /// Serializa um objeto para uma string XML
        /// </summary>
        /// <typeparam name="TEntity">Tipo que será serializado</typeparam>
        /// <param name="obj">Objeto qe será submetido à Serialização</param>
        /// <returns></returns>
        internal static string XmlSerialize<TEntity>(this TEntity obj) where TEntity : class
        {
            using (StringWriter writer = new StringWriter())
            {
                XmlSerializer s = new XmlSerializer(typeof(TEntity));
                s.Serialize(writer, obj);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Serializa um objeto para uma string JSON
        /// </summary>
        /// <typeparam name="TEntity">Tipo que será serializado</typeparam>
        /// <param name="obj">Objeto qe será submetido à Serialização</param>
        /// <returns></returns>
        internal static string JsonSerialize<TEntity>(this TEntity obj) where TEntity : class
        {
            return (new JavaScriptSerializer()).Serialize(obj);
        }
    }

    #endregion

}