using NMockito.Mocks;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NMockito.Expectations {
   public class TrainedMockFactory {
      private readonly MockFactory mockFactory;
      private readonly NMockitoCore core;

      public TrainedMockFactory(MockFactory mockFactory, NMockitoCore core) {
         this.mockFactory = mockFactory;
         this.core = core;
      }

      public T Create<T>(Expression<Func<T, bool>> trainingExpression) where T : class {
         var mock = mockFactory.CreateMock<T>();
         var trainingExpressionBody = (BinaryExpression)trainingExpression.Body;
         DescendTrainingExpressionTree(mock, (BinaryExpression)trainingExpressionBody);
         return mock;
      }

      public void DescendTrainingExpressionTree(object mock, BinaryExpression expression) {
         var left = expression.Left;
         var right = expression.Right;
         switch (expression.NodeType) {
            case ExpressionType.AndAlso:
               DescendTrainingExpressionTree(mock, (BinaryExpression)left);
               DescendTrainingExpressionTree(mock, (BinaryExpression)right);
               break;
            case ExpressionType.Equal:
               if (left.NodeType == ExpressionType.Convert) {
                  left = ((UnaryExpression)left).Operand;
               }
               var value = Expression.Lambda(right).Compile().DynamicInvoke();
               HandlePropertyTrainingExpression(mock, (MemberExpression)left, value);
               break;
         }
      }

      private void HandlePropertyTrainingExpression(object mock, MemberExpression memberExpression, object value) {
         var property = (PropertyInfo)memberExpression.Member;
         var propertyGetter = property.GetGetMethod();
         core.Expect(() => propertyGetter.Invoke(mock, null)).ThenReturn(value);
      }
   }
}
