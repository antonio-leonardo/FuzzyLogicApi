using System;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using DynamicLinq = System.Linq.Dynamic;

namespace FuzzyLogicApi
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public sealed class FuzzyLogic<TEntity> where TEntity : class, new()
    {
        /// <summary>
        /// 
        /// </summary>
        private Type TypeOf { get { return typeof(TEntity); } }

        /// <summary>
        /// 
        /// </summary>
        private static TEntity[] Collection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private readonly Func<TEntity, bool> _function;

        /// <summary>
        /// 
        /// </summary>
        private readonly Predicate<TEntity> _predicate;

        /// <summary>
        /// 
        /// </summary>
        private readonly string[] _propertiesNames;

        /// <summary>
        /// 
        /// </summary>
        private Predicate<TEntity> _negatePredicate { get { return x => !_predicate(x); } }

        /// <summary>
        /// 
        /// </summary>
        private readonly Expression<Func<TEntity, bool>> _expression;

        /// <summary>
        /// 
        /// </summary>
        private InferenceResult<TEntity> InferenceResult { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string[] BrokedBooleanExpressionsByOrOperator { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string[] ExpressionParameters { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public BinaryExpression[] BrokedExpressions { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ReadOnlyCollection<ParameterExpression> ParametersCollection { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        private FuzzyLogic(Expression<Func<TEntity, bool>> expression)
        {
            this._function = expression.Compile();
            this._predicate = (entity) => this._function.Invoke(entity);
            this._expression = expression;

            this.GetBrokedBooleanExpressionsByOrOperator();

            this.ExpressionParameters = this.GetExpressionParameters().ToArray();
            this.ParametersCollection = _expression.Parameters;

            PropertyInfo[] props = this.TypeOf.GetProperties();
            List<string> propsNames = new List<string>();
            for (int i = 0; i < props.Length; i++)
            {
                propsNames.Add(props[i].Name);
            }
            this._propertiesNames = propsNames.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        private void GetBrokedBooleanExpressionsByOrOperator()
        {
            ExpressionModel OrTree = new ExpressionModel();
            this.GetExpressionTree(this._expression.Body as BinaryExpression, OrTree);
            List<string> finalResult = new List<string>();
            List<BinaryExpression> finalExpressions = new List<BinaryExpression>();
            this.GetAllValidConditionalsByRightExpression(OrTree, ref finalResult, ExpressionType.OrElse);
            this.BrokedBooleanExpressionsByOrOperator = finalResult.ToArray();
            this.GetAllValidExpressions(OrTree, ref finalExpressions, ExpressionType.OrElse);
            this.BrokedExpressions = finalExpressions.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="type"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static string GetInferenceResult(Expression<Func<TEntity, bool>> expression, ResponseType type, List<TEntity> collection)
        {
            Collection = collection.ToArray();
            return (new FuzzyLogic<TEntity>(expression)).GetInferenceResult(type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="collection"></param>
        public static string GetInferenceResult(Expression<Func<TEntity, bool>> expression, ResponseType type, params TEntity[] collection)
        {
            Collection = collection;
            return (new FuzzyLogic<TEntity>(expression)).GetInferenceResult(type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static InferenceResult<TEntity> GetInferenceResult(Expression<Func<TEntity, bool>> expression, params TEntity[] collection)
        {
            Collection = collection;
            return (new FuzzyLogic<TEntity>(expression)).GetInferenceResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static InferenceResult<TEntity> GetInferenceResult(Expression<Func<TEntity, bool>> expression, List<TEntity> collection)
        {
            Collection = collection.ToArray();
            return (new FuzzyLogic<TEntity>(expression)).GetInferenceResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="type"></param>
        /// <param name="entityItem"></param>
        /// <returns></returns>
        public static string GetInferenceResult(Expression<Func<TEntity, bool>> expression, ResponseType type, TEntity entityItem)
        {
            string result = null;
            switch (type)
            {
                case ResponseType.Xml:
                    result = (new FuzzyLogic<TEntity>(expression)).GetBruteInfereceCollection(entityItem, 0).XmlSerialize();
                    break;
                case ResponseType.Json:
                    result = (new FuzzyLogic<TEntity>(expression)).GetBruteInfereceCollection(entityItem, 0).JsonSerialize();
                    break;
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="entityItem"></param>
        /// <returns></returns>
        public static Inferecence<TEntity> GetInferenceResult(Expression<Func<TEntity, bool>> expression, TEntity entityItem)
        {
            return (new FuzzyLogic<TEntity>(expression)).GetBruteInfereceCollection(entityItem, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string GetInferenceResult(ResponseType type)
        {
            string returning = null;
            if (null == this.InferenceResult)
            {
                Inferecence<TEntity>[] inferences = this.GenerateInferenceCollection().ToArray();
                this.InferenceResult = new InferenceResult<TEntity>()
                {
                    Inferences = inferences
                };
            }
            switch (type)
            {
                case ResponseType.Xml:
                    returning = this.InferenceResult.XmlSerialize();
                    break;
                case ResponseType.Json:
                    returning = this.InferenceResult.JsonSerialize();
                    break;
            }
            return returning;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private InferenceResult<TEntity> GetInferenceResult()
        {
            InferenceResult<TEntity> result = new InferenceResult<TEntity>();
            if (null == this.InferenceResult)
            {
                Inferecence<TEntity>[] inferences = this.GenerateInferenceCollection().ToArray();
            }
            result.Inferences = this.InferenceResult.Inferences;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetExpressionParameters()
        {
            ReadOnlyCollection<ParameterExpression> externalParam = _expression.Parameters;
            foreach (ParameterExpression item in externalParam)
            {
                yield return item.Name;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Inferecence<TEntity>> GenerateInferenceCollection()
        {
            TEntity[] notValidValues = this.FindAllNotValid().ToArray();
            for (int i = 0; i < notValidValues.Length; i++)
            {
                yield return this.GetBruteInfereceCollection(notValidValues[i], i);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        private Inferecence<TEntity> GetBruteInfereceCollection(TEntity entity, int Id)
        {
            Inferecence<TEntity> result = new Inferecence<TEntity>();
            result.RatingsReport = new List<bool>();
            result.PropertiesNeedToChange = new List<string>();
            result.Data = entity;
            result.ID = Id;

            List<IntermediateInferenceItem> intermediateInference = new List<IntermediateInferenceItem>();
            for (int i = 0; i < this.BrokedBooleanExpressionsByOrOperator.Length; i++)
            {
                string[] breakApartExpressionByAndAlso = this.BrokedBooleanExpressionsByOrOperator[i].Split(new string[] { " AndAlso " }, StringSplitOptions.None);
                IntermediateInferenceItem item = new IntermediateInferenceItem()
                {
                    PropertiesNeedToChange = new List<string>(),
                    RatingsReport = new List<bool>()
                };
                for (int j = 0; j < breakApartExpressionByAndAlso.Length; j++)
                {
                    for (int x = 0; x < this._propertiesNames.Length; x++)
                    {
                        if (breakApartExpressionByAndAlso[j].Contains(this._propertiesNames[x]))
                        {
                            bool rating = this.ProcessFilter(breakApartExpressionByAndAlso[j], entity, this._propertiesNames[x]);
                            item.RatingsReport.Add(rating);
                            if (!rating)
                            {
                                item.PropertiesNeedToChange.Add(this._propertiesNames[x]);
                            }
                        }
                    }
                }
                intermediateInference.Add(item);
            }
            IntermediateInferenceItem minErrors = intermediateInference.OrderBy(x => x.ErrorsQuantity).FirstOrDefault();
            result.RatingsReport = minErrors.RatingsReport;
            result.PropertiesNeedToChange = minErrors.PropertiesNeedToChange;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="invalidData"></param>
        /// <returns></returns>
        private bool ProcessFilter(string filter, TEntity invalidData, string propertyName)
        {
            if(typeof(TEntity).GetProperty(propertyName).PropertyType == typeof(Double) && filter.Contains(","))
            {
                filter = filter.Replace(",", ".");
            }
            bool ratingResult = false;
            const char FIRST_BRACKET = '(',
                       LAST_BRACKET = ')';
            string newFilter = filter;
            List<int> charsIdWithStartParenteses = new List<int>();
            List<int> charsIdWithEndParenteses = new List<int>();
            char[] filterChars = filter.ToCharArray();
            for (int i = 0; i < filterChars.Length; i++)
            {
                if (filterChars[i] == FIRST_BRACKET)
                {
                    charsIdWithStartParenteses.Add(i);
                }
                else if (filterChars[i] == LAST_BRACKET)
                {
                    charsIdWithEndParenteses.Add(i);
                }
            }
            bool isMatch = false;
            while (!isMatch)
            {
                try
                {
                    this.SubstituteChars(ref newFilter);
                    ratingResult = DynamicLinq.DynamicExpression.ParseLambda<TEntity, bool>(newFilter).Compile().Invoke(invalidData);
                    isMatch = true;
                }
                catch
                {
                    if (charsIdWithStartParenteses.Count > charsIdWithEndParenteses.Count)
                    {
                        filter = filter.Remove(filter.IndexOf(FIRST_BRACKET), 1);
                        newFilter = filter;
                        charsIdWithStartParenteses.Remove(charsIdWithStartParenteses.FirstOrDefault());
                    }
                    else if (charsIdWithStartParenteses.Count < charsIdWithEndParenteses.Count)
                    {
                        filter = filter.Remove(filter.LastIndexOf(LAST_BRACKET) - 1, 1);
                        newFilter = filter;
                        charsIdWithEndParenteses.Remove(charsIdWithEndParenteses.LastOrDefault());
                    }
                    else
                    {
                        if (charsIdWithStartParenteses.SequenceEqual(Enumerable.Range(1, charsIdWithStartParenteses.Count)))
                        {
                            filter = filter.Remove(filter.LastIndexOf(LAST_BRACKET) - 1, 1);
                            newFilter = filter;
                            charsIdWithEndParenteses.Remove(charsIdWithEndParenteses.LastOrDefault());
                        }
                        else if (charsIdWithEndParenteses.SequenceEqual(Enumerable.Range(1, charsIdWithEndParenteses.Count)))
                        {
                            filter = filter.Remove(filter.IndexOf(FIRST_BRACKET), 1);
                            newFilter = filter;
                            charsIdWithStartParenteses.Remove(charsIdWithStartParenteses.FirstOrDefault());
                        }
                    }
                }
            }
            return ratingResult;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        private void AddParametersOnFilter(ref string filter)
        {
            filter = " => " + filter;
            for (int i = 0; i < ExpressionParameters.Length; i++)
            {
                filter = "(" + ExpressionParameters[i] + ")" + filter;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        private void SubstituteChars(ref string filter)
        {
            filter = filter.Replace("IsNullOrWhiteSpace", "string.IsNullOrWhiteSpace")
                           .Replace("AndAlso", "&&")
                           .Replace("OrElse", "||")
                           .Replace("Not", "!");
            foreach (string item in this.ExpressionParameters)
            {
                filter = filter.Replace(item + ".", "");
                if (!(item.Length == 1 && (filter.Split(item.FirstOrDefault()).Length -1) > 1))
                {
                    filter.Replace(item, "");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="tree"></param>
        private void GetExpressionTree(BinaryExpression expression, ExpressionModel tree)
        {
            tree.BinaryExpressionItem = expression;
            tree.Data = expression.ToString();
            BinaryExpression left = expression.Left as BinaryExpression;
            BinaryExpression right = expression.Right as BinaryExpression;
            if (null != left)
            {
                tree.LeftExpression = new ExpressionModel();
                this.GetExpressionTree(expression.Left as BinaryExpression, tree.LeftExpression);
            }
            if (null != right)
            {
                tree.RightExpression = new ExpressionModel();
                this.GetExpressionTree(expression.Right as BinaryExpression, tree.RightExpression);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="finalResult"></param>
        private void GetAllValidConditionalsByRightExpression(ExpressionModel tree, ref List<string> finalResult, ExpressionType selectedType)
        {
            if (null != tree.RightExpression)
            {
                if (tree.Data.Replace(tree.RightExpression.Data + ")", "").EndsWith(selectedType.ToString() + " "))
                {
                    finalResult.Add(tree.RightExpression.Data);
                    if (null != tree.LeftExpression)
                    {
                        this.GetAllValidConditionalsByRightExpression(tree.LeftExpression, ref finalResult, selectedType);
                    }
                }
                else
                {
                    finalResult.Add(tree.Data);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="finalExpressions"></param>
        private void GetAllValidExpressions(ExpressionModel tree, ref List<BinaryExpression> finalExpressions, ExpressionType selectedType)
        {
            if (null != tree.RightExpression)
            {
                if (tree.Data.Replace(tree.RightExpression.Data + ")", "").EndsWith(selectedType.ToString() + " "))
                {
                    finalExpressions.Add(tree.RightExpression.BinaryExpressionItem);
                    if (null != tree.LeftExpression)
                    {
                        this.GetAllValidExpressions(tree.LeftExpression, ref finalExpressions, selectedType);
                    }
                }
                else
                {
                    finalExpressions.Add(tree.BinaryExpressionItem);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TEntity[] FindAllValids()
        {
            return Array.FindAll(Collection, this._predicate);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public TEntity[] FindAllNotValid()
        {
            return Array.FindAll(Collection, this._negatePredicate);
        }
    }
}