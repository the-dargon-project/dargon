using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito;
using NMockito.Utilities;
using Xunit;

namespace Dargon.Commons {
   public class ReflectionTests : NMockitoInstance {
      [Fact]
      public void ZeroAndDefaultReconstructTest() {
         var inst = new TestClass();
         Check(inst);
         ReflectionUtils.Zero(inst);
         Check(inst, 0, 0, 0, null, null, null, false, false, false, 0.0f, 0.0, false);
         ReflectionUtils.DefaultReconstruct(inst);
         Check(inst);
      }

      public void Check(TestClass inst, int ia = 0, int ib = 10, int ic = 20, int? da = null, int? db = 10, int? dc = 20, bool gaNonDefault = false, bool gbNonDefault = true, bool gcNonDefault = true, float f = 2.0f, double d = 1.0, bool b = true) {
         AssertEquals(ia, inst.IA);
         AssertEquals(ib, inst.IB);
         AssertEquals(ic, inst.IC);

         AssertEquals(da.HasValue ? (object)da.Value : null, inst.DA != null ? (object)inst.DA.Value : null);
         AssertEquals(db.HasValue ? (object)db.Value : null, inst.DB != null ? (object)inst.DB.Value : null);
         AssertEquals(dc.HasValue ? (object)dc.Value : null, inst.DC != null ? (object)inst.DC.Value : null);

         AssertTrue((inst.GuidA != default) == gaNonDefault);
         AssertTrue((inst.GuidB != default) == gbNonDefault);
         AssertTrue((inst.GuidC != default) == gcNonDefault);

         AssertEquals(f, inst.f);
         AssertEquals(d, inst.d);
         AssertEquals(b, inst.b);
      }

      public class TestClass {
         public int IA { get; set; }
         public int IB { get; } = 10;
         public int IC = 20;

         public Dummy DA { get; set; }
         public Dummy DB { get; } = new Dummy { Value = 10 };
         public Dummy DC = new Dummy { Value = 20 };

         public Guid GuidA { get; set; }
         public Guid GuidB { get; } = Guid.NewGuid();
         public readonly Guid GuidC = Guid.NewGuid();

         public float f = 2.0f;
         public double d = 1.0;
         public bool b = true;
      }

      public class Dummy {
         public int Value;
      }
   }
}
