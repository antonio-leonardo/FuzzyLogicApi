using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        private readonly string[] _propertiesNames;

        /// <summary>
        /// 
        /// </summary>
        private readonly Expression<Func<TEntity, bool>> _expression;

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
        /// <param name="type"></param>
        /// <param name="entityItem"></param>
        /// <returns></returns>
        public static string GetInference(Expression<Func<TEntity, bool>> expression, ResponseType type, IEnumerable<TEntity> entities)
        {
            string result = null;
            InferenceResult<TEntity> inferences = new InferenceResult<TEntity>()
            {
                Inferences = (new FuzzyLogic<TEntity>(expression)).InferenceEntities(entities).ToArray()
            };
            switch (type)
            {
                case ResponseType.Xml:
                    result = inferences.XmlSerialize();
                    break;
                case ResponseType.Json:
                    result = inferences.JsonSerialize();
                    break;
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        private IEnumerable<Inference<TEntity>> InferenceEntities(IEnumerable<TEntity> entities)
        {
            foreach (TEntity entity in entities)
            {
                InferenceInternal<TEntity> inferenceResultInternal = this.GetBruteInferenceCollection(entity);
                yield return inferenceResultInternal.InternalToPublicConversion();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="type"></param>
        /// <param name="entityItem"></param>
        /// <returns></returns>
        public static InferenceResult<TEntity> GetInference(Expression<Func<TEntity, bool>> expression, IEnumerable<TEntity> entities)
        {
            return new InferenceResult<TEntity>()
            {
                Inferences = (new FuzzyLogic<TEntity>(expression)).InferenceEntities(entities).ToArray()
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="type"></param>
        /// <param name="entityItem"></param>
        /// <returns></returns>
        public static string GetInference(Expression<Func<TEntity, bool>> expression, ResponseType type, TEntity entityItem)
        {
            string result = null;
            InferenceInternal<TEntity> InferenceResultInternal = (new FuzzyLogic<TEntity>(expression)).GetBruteInferenceCollection(entityItem);
            Inference<TEntity> inferenceResult = InferenceResultInternal.InternalToPublicConversion();
            switch (type)
            {
                case ResponseType.Xml:
                    result = inferenceResult.XmlSerialize();
                    break;
                case ResponseType.Json:
                    result = inferenceResult.JsonSerialize();
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
        public static Inference<TEntity> GetInference(Expression<Func<TEntity, bool>> expression, TEntity entityItem)
        {
            return (new FuzzyLogic<TEntity>(expression)).GetBruteInferenceCollection(entityItem).InternalToPublicConversion();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        private FuzzyLogic(Expression<Func<TEntity, bool>> expression)
        {
            this._expression = expression;
            this.GetBrokedBooleanExpressionsByOrOperator();
            this.ExpressionParameters = this.GetExpressionParameters().ToArray();
            this.ParametersCollection = this._expression.Parameters;
            this._propertiesNames = this.GetPropertiesNames().ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetPropertiesNames()
        {
            PropertyInfo[] props = this.TypeOf.GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                yield return props[i].Name;
            }
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
        /// <returns></returns>
        private IEnumerable<string> GetExpressionParameters()
        {
            ReadOnlyCollection<ParameterExpression> externalParam = this._expression.Parameters;
            foreach (ParameterExpression item in externalParam)
            {
                yield return item.Name;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private IEnumerable<IntermediateInferenceItemInternal> GetBruteInferenceCollectionIntermediate(TEntity entity)
        {
            for (int i = 0; i < this.BrokedBooleanExpressionsByOrOperator.Length; i++)
            {
                IntermediateInferenceItemInternal item = new IntermediateInferenceItemInternal()
                {
                    PropertiesNeedToChange = new List<string>(),
                    RatingsReport = new List<bool>()
                };
                string[] breakApartExpressionByAndAlso = this.BrokedBooleanExpressionsByOrOperator[i].Split(new string[] { " AndAlso " }, StringSplitOptions.None);
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
                yield return item;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private InferenceInternal<TEntity> GetBruteInferenceCollection(TEntity entity)
        {
            InferenceInternal<TEntity> result = new InferenceInternal<TEntity>()
            {
                RatingsReport = new List<bool>(),
                PropertiesNeedToChange = new List<string>(),
                Data = entity
            };
            IntermediateInferenceItemInternal minErrors = GetBruteInferenceCollectionIntermediate(entity).OrderBy(x => x.ErrorsQuantity).FirstOrDefault();
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
            if (typeof(TEntity).GetProperty(propertyName).PropertyType == typeof(Double) && filter.Contains(","))
            {
                filter = filter.Replace(",", ".");
            }
            bool ratingResult = false;
            const char FIRST_BRACKET = '(',
                       LAST_BRACKET = ')';
            string newFilter = filter;

            ConcurrentBag<int> charsIdWithStartParentesesParallel = new ConcurrentBag<int>();
            ConcurrentBag<int> charsIdWithEndParentesesParallel = new ConcurrentBag<int>();
            char[] filterChars = filter.ToCharArray();
            Parallel.For(0, filterChars.Length, i =>
            {
                if (filterChars[i] == FIRST_BRACKET)
                {
                    charsIdWithStartParentesesParallel.Add(i);
                }
                else if (filterChars[i] == LAST_BRACKET)
                {
                    charsIdWithEndParentesesParallel.Add(i);
                }
            });

            List<int> charsIdWithStartParenteses = charsIdWithStartParentesesParallel.ToList();
            List<int> charsIdWithEndParenteses = charsIdWithEndParentesesParallel.ToList();

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
        private void SubstituteChars(ref string filter)
        {
            filter = filter.Replace("IsNullOrWhiteSpace", "string.IsNullOrWhiteSpace")
                           .Replace("AndAlso", "&&")
                           .Replace("OrElse", "||")
                           .Replace("Not", "!");
            foreach (string item in this.ExpressionParameters)
            {
                filter = filter.Replace(item + ".", "");
                if (!(item.Length == 1 && (filter.Split(item.FirstOrDefault()).Length - 1) > 1))
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
    }
}