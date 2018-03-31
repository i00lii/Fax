using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FluentAssertions
{
	internal static class StructuralAssertions
	{
		public static void ShouldHaveMatchingItems<TItem>( this IReadOnlyCollection<TItem> collection, IReadOnlyCollection<TItem> expectedCollection ) =>
			collection.ShouldHaveMatchingItemsInternal( expectedCollection, String.Empty );

		private static void ShouldHaveMatchingItemsInternal<TItem>( this IReadOnlyCollection<TItem> collection, IReadOnlyCollection<TItem> expectedCollection, string path )
		{
			collection.Should().HaveCount( expectedCollection.Count, path );
			var itemPairs = collection.Zip( expectedCollection, ( actual, expected ) => new { actual, expected } );

			int index = 0;
			foreach ( var itemPair in itemPairs )
			{
				itemPair.actual.ShouldMatch( itemPair.expected, $"{path}[{index}]" );
				index++;
			}
		}

		public static void ShouldHaveMatchingProperties<T>( this T actualObject, T expectedObject ) => actualObject.ShouldHaveMatchingPropertiesInternal( expectedObject, String.Empty );

		private static void ShouldHaveMatchingPropertiesInternal<T>( this T actualObject, T expectedObject, string path )
		{
			PropertyInfo[] properties = typeof( T ).GetProperties( BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty );
			foreach ( PropertyInfo property in properties )
			{
				object expectedValue = property.GetValue( expectedObject );
				if ( IsDefault( expectedValue ) )
					continue;

				object actualValue = property.GetValue( actualObject );

				MethodInfo shouldMatchMethod = typeof( StructuralAssertions )
					.GetMethod( nameof( ShouldMatch ), BindingFlags.Static | BindingFlags.NonPublic )
					.GetGenericMethodDefinition()
					.MakeGenericMethod( property.PropertyType );

				Action<object, object, object> shouldMatchAction = CompileToAction( shouldMatchMethod );
				shouldMatchAction( actualValue, expectedValue, $"{path}.{property.Name}" );
			}

			bool IsDefault( object expectedValue )
				=> expectedValue == null
				|| expectedValue.Equals( BoxDefaultValue( expectedValue.GetType() ) );

			object BoxDefaultValue( Type type )
				=> Expression
				.Lambda<Func<object>>( Expression.Convert( Expression.Default( type ), typeof( object ) ) )
				.Compile()
				.Invoke();
		}

		private static void ShouldMatch<T>( this T actualValue, T expectedValue, string path )
		{
			string reason = $"that's what expected at {path}";

			if ( IsEquatable() || actualValue is Type )
			{
				actualValue.Should().Be( expectedValue, reason );
				return;
			}

			if ( ReferenceEquals( expectedValue, null ) )
			{
				actualValue.Should().BeNull( reason );
				return;
			}

			actualValue.Should().NotBeNull( reason );

			if ( IsCollection( out Type itemType ) )
			{
				MethodInfo shouldHaveMatchingItemsMethod = typeof( StructuralAssertions )
					.GetMethod( nameof( ShouldHaveMatchingItemsInternal ), BindingFlags.Static | BindingFlags.NonPublic )
					.GetGenericMethodDefinition()
					.MakeGenericMethod( itemType );

				Action<object, object, object> shouldMatchAction = CompileToAction( shouldHaveMatchingItemsMethod );
				shouldMatchAction( actualValue, expectedValue, path );
				return;
			}

			actualValue.ShouldHaveMatchingPropertiesInternal( expectedValue, path );

			bool IsEquatable() => typeof( IEquatable<T> ).IsAssignableFrom( typeof( T ) );

			bool IsCollection( out Type underlyingType )
			{
				underlyingType = typeof( T )
					.GetInterfaces()
					.Concat( new[] { typeof( T ) } )
					.FirstOrDefault( i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof( IReadOnlyCollection<> ) )
					?.GetGenericArguments()[ 0 ];
				return underlyingType != default;
			}
		}

		private static Action<object, object, object> CompileToAction( MethodInfo method )
		{
			ParameterExpression[] paramExpressions = method
				.GetParameters()
				.Select( ( param, index ) => Expression.Parameter( typeof( object ), $"param{index}" ) )
				.ToArray();

			MethodCallExpression body = Expression
				.Call
				(
					null,
					method,
					method.GetParameters().Select( ( param, index ) => Expression.Convert( paramExpressions[ index ], param.ParameterType ) )
				);

			Expression<Action<object, object, object>> lambdaExpression = Expression.Lambda<Action<object, object, object>>( body, paramExpressions );
			return lambdaExpression.Compile();
		}
	}
}
