//-----------------------------------------------------------------------
// <copyright file="LinqExtensions.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
#if !NET_3_5
using System.Threading.Tasks;
#endif
using Raven.Abstractions.Data;
using Raven.Client.Client;

namespace Raven.Client.Linq
{
	using System.Linq.Expressions;
	using Database.Data;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	///<summary>
	/// Extensions to the linq syntax
	///</summary>
	public static partial class LinqExtensions
	{
		/// <summary>
		/// Project using a different type
		/// </summary>
		public static IEnumerable<TResult> As<TResult>(this IQueryable queryable)
		{
			var results = queryable.Provider.CreateQuery<TResult>(queryable.Expression);
			var ravenQueryInspector = ((RavenQueryInspector<TResult>)results);
			ravenQueryInspector.Customize(x => x.CreateQueryForSelectedFields<TResult>(null));
			return results;
		}

		/// <summary>
		/// Project using a different type
		/// </summary>
		public static IRavenQueryable<TResult> AsProjection<TResult>(this IQueryable queryable)
		{
			var results = queryable.Provider.CreateQuery<TResult>(queryable.Expression);
			var ravenQueryInspector = ((RavenQueryInspector<TResult>)results);
			ravenQueryInspector.FieldsToFetch(typeof(TResult).GetProperties().Select(x => x.Name));
			ravenQueryInspector.Customize(x => x.CreateQueryForSelectedFields<TResult>(null));
			return (IRavenQueryable<TResult>)results;
		}
#if !SILVERLIGHT

		/// <summary>
		/// Suggest alternative values for the queried term
		/// </summary>
		public static SuggestionQueryResult Suggest(this IQueryable queryable)
		{
			return Suggest(queryable, new SuggestionQuery());
		}

		/// <summary>
		/// Suggest alternative values for the queried term
		/// </summary>
		public static SuggestionQueryResult Suggest(this IQueryable queryable, SuggestionQuery query)
		{
			var ravenQueryInspector = ((IRavenQueryInspector)queryable);
			SetSuggestionQueryFieldAndTerm(ravenQueryInspector,query);
			return ravenQueryInspector.DatabaseCommands.Suggest(ravenQueryInspector.IndexQueried, query);
		}
#endif

		private static void SetSuggestionQueryFieldAndTerm(IRavenQueryInspector queryInspector, SuggestionQuery query)
		{
			var lastEqualityTerm = queryInspector.GetLastEqualityTerm();
			if (lastEqualityTerm.Key == null)
				throw new InvalidOperationException("Could not suggest on a query that doesn't have a single equality check");

			query.Field = lastEqualityTerm.Key;
			query.Term = lastEqualityTerm.Value;
		}
#if !NET_3_5
		/// <summary>
		/// Suggest alternative values for the queried term
		/// </summary>
		public static Task<SuggestionQueryResult> SuggestAsync(this IQueryable queryable, SuggestionQuery query)
		{
			var ravenQueryInspector = ((IRavenQueryInspector)queryable);
			SetSuggestionQueryFieldAndTerm(ravenQueryInspector, query);

			return ravenQueryInspector.AsyncDatabaseCommands.SuggestAsync(ravenQueryInspector.IndexQueried, query);
		}

		/// <summary>
		/// Suggest alternative values for the queried term
		/// </summary>
		public static Task<SuggestionQueryResult> SuggestAsync(this IQueryable queryable)
		{
			return SuggestAsync(queryable, new SuggestionQuery());
		}
#endif


		/// <summary>
		/// Marker method for allowing complex (multi entity) queries on the server.
		/// </summary>
		public static IEnumerable<TResult> WhereEntityIs<TResult>(this IEnumerable<object> queryable, params string[] names)
		{
			throw new NotSupportedException("This method is provided solely to allow query translation on the server");
		}

		/// <summary>
		/// Marker method for allowing hierarchical queries on the server.
		/// </summary>
		public static IEnumerable<TResult> Hierarchy<TResult>(this TResult item, string path)
		{
			throw new NotSupportedException("This method is provided solely to allow query translation on the server");
		}

		/// <summary>
		/// Marker method for allowing hierarchical queries on the server.
		/// </summary>
		public static IEnumerable<TResult> Hierarchy<TResult>(this TResult item, Func<TResult, IEnumerable<TResult>> path)
		{
			throw new NotSupportedException("This method is provided solely to allow query translation on the server");
		}

#if !NET_3_5
		/// <summary>
		/// Returns a list of results for a query asynchronously. 
		/// </summary>
		public static Task<IList<T>> ToListAsync<T>(this IRavenQueryable<T> source)
		{
			var provider = (IRavenQueryProvider)source.Provider;
			return provider.ToLuceneQuery<T>(source.Expression).ToListAsync();
		} 
#endif

		/// <summary>
		/// Includes the specified path in the query, loading the document specified in that path
		/// </summary>
		/// <param name="path">The path.</param>
		public static IRavenQueryable<T> Include<T>(this IRavenQueryable<T> source, Expression<Func<T, object>> path)
		{
			source.Customize(x => x.Include(path));
			return source;
		}

		/// <summary>
		/// Filters a sequence of values based on a predicate.
		/// </summary>
		public static IRavenQueryable<T> Where<T>(this IRavenQueryable<T> source, Expression<Func<T, bool>> prediate)
		{
			return (IRavenQueryable<T>)Queryable.Where(source, prediate);
		}

		/// <summary>
		/// Filters a sequence of values based on a predicate.
		/// </summary>
		public static IRavenQueryable<T> Where<T>(this IRavenQueryable<T> source, Expression<Func<T, int, bool>> prediate)
		{
			return (IRavenQueryable<T>)Queryable.Where(source, prediate);
		}

		/// <summary>
		/// Sorts the elements of a sequence in ascending order according to a key.
		/// </summary>
		public static IRavenQueryable<T> OrderBy<T, TK>(this IRavenQueryable<T> source, Expression<Func<T, TK>> keySelector)
		{
			return (IRavenQueryable<T>)Queryable.OrderBy(source, keySelector);
		}

		/// <summary>
		/// Sorts the elements of a sequence in ascending order according to a key.
		/// </summary>
		public static IRavenQueryable<T> OrderBy<T, TK>(this IRavenQueryable<T> source, Expression<Func<T, TK>> keySelector, IComparer<TK> comparer)
		{
			return (IRavenQueryable<T>)Queryable.OrderBy(source, keySelector, comparer);
		}

		/// <summary>
		/// Sorts the elements of a sequence in descending order according to a key.
		/// </summary>
		public static IRavenQueryable<T> OrderByDescending<T, TK>(this IRavenQueryable<T> source, Expression<Func<T, TK>> keySelector)
		{
			return (IRavenQueryable<T>)Queryable.OrderByDescending(source, keySelector);
		}

		/// <summary>
		/// Sorts the elements of a sequence in descending order according to a key.
		/// </summary>
		public static IRavenQueryable<T> OrderByDescending<T, TK>(this IRavenQueryable<T> source, Expression<Func<T, TK>> keySelector, IComparer<TK> comparer)
		{
			return (IRavenQueryable<T>)Queryable.OrderByDescending(source, keySelector, comparer);
		}

		/// <summary>
		/// Projects each element of a sequence into a new form.
		/// </summary>
		public static IRavenQueryable<TResult> Select<TSource, TResult>(this IRavenQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
		{
			return (IRavenQueryable<TResult>)Queryable.Select(source, selector);
		}

		/// <summary>
		/// Projects each element of a sequence into a new form.
		/// </summary>
		public static IRavenQueryable<TResult> Select<TSource, TResult>(this IRavenQueryable<TSource> source, Expression<Func<TSource,int, TResult>> selector)
		{
			return (IRavenQueryable<TResult>)Queryable.Select(source, selector);
		}
	}
}
