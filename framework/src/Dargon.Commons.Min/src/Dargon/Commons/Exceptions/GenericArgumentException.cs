using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons.Exceptions {
   public class GenericArgumentException<T> : Exception {
      public GenericArgumentException() : base($"`{typeof(T).FullName}` is an invalid generic argument.") {

      }
   }
}
