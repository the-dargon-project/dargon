using System;
using System.CodeDom;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using Dargon.PortableObjects;
using ItzWarty;
using ItzWarty.Collections;
using NMockito;
using Xunit;
using SCG = System.Collections.Generic;

namespace Dargon.Ryu {
   public class RyuContainerImplTests : NMockitoInstance {
      [Mock] private readonly IPofContext pofContext = null;
      [Mock] private readonly IPofSerializer pofSerializer = null;

      [Mock] private readonly SCG.IDictionary<Type, RyuPackageV1TypeInfo> typeInfosByType = null;
      [Mock] private readonly IConcurrentDictionary<Type, object> instancesByType = null;
      [Mock] private readonly SCG.ISet<Type> remoteServices = null;

      private readonly RyuContainerImpl testObj;

      public RyuContainerImplTests() {
         testObj = new RyuContainerImpl(pofContext, pofSerializer, typeInfosByType, instancesByType, remoteServices);
         testObj.Setup();
         ClearInteractions();
      }

      [Fact]
      public void Set_NoTypeInfo_Test() {
         var dummy = CreateMock<DummyInterface>();
         RyuPackageV1TypeInfo typeInfo;
         When(typeInfosByType.TryGetValue(typeof(DummyInterface), out typeInfo)).ThenReturn(false);
         testObj.Set<DummyInterface>(dummy);
         Verify(typeInfosByType).TryGetValue(typeof(DummyInterface), out typeInfo);
         Verify(instancesByType)[typeof(DummyInterface)] = dummy;
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void Set_CachedType_Test() {
         var dummy = CreateMock<DummyInterface>();
         var typeInfo = CreateMock<RyuPackageV1TypeInfo>();
         var typeInfoPlaceHolder = CreateMock<RyuPackageV1TypeInfo>();
         When(typeInfo.Flags).ThenReturn(RyuTypeFlags.Cache);
         When(typeInfosByType.TryGetValue(typeof(DummyInterface), out typeInfoPlaceHolder)).Set(typeInfoPlaceHolder, typeInfo).ThenReturn(true);
         testObj.Set<DummyInterface>(dummy);
         Verify(typeInfo).Flags.Wrap();
         Verify(typeInfosByType).TryGetValue(typeof(DummyInterface), out typeInfo);
         Verify(instancesByType)[typeof(DummyInterface)] = dummy;
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void Set_NonCachedType_Test() {
         var dummy = CreateMock<DummyInterface>();
         var typeInfo = CreateMock<RyuPackageV1TypeInfo>();
         var typeInfoPlaceHolder = CreateMock<RyuPackageV1TypeInfo>();
         When(typeInfo.Flags).ThenReturn(RyuTypeFlags.None);
         When(typeInfosByType.TryGetValue(typeof(DummyInterface), out typeInfoPlaceHolder)).Set(typeInfoPlaceHolder, typeInfo).ThenReturn(true);
         AssertThrows<InvalidOperationException>(() => testObj.Set(dummy));
         Verify(typeInfo).Flags.Wrap();
         Verify(typeInfosByType).TryGetValue(typeof(DummyInterface), out typeInfo);
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void Find_Generic_HappyPathTest() {
         var a = new DummyImplementationA();
         var b = new DummyImplementationB();
         var instances = ImmutableDictionary.Of(
            typeof(DummyInterface), a,
            typeof(DummyImplementationA), a,
            typeof(DummyImplementationB), b,
            typeof(object), new object());
         var instancesByTypeLinq = (SCG.IEnumerable<SCG.KeyValuePair<Type, object>>)instancesByType;

         When(instancesByTypeLinq.GetEnumerator()).ThenReturn(instances.GetEnumerator());

         var matches = testObj.Find<DummyInterface>().ToList();

         Verify(instancesByTypeLinq).GetEnumerator();
         VerifyNoMoreInteractions();

         AssertEquals(2, matches.Count);
         AssertTrue(matches.Contains(a));
         AssertTrue(matches.Contains(b));
      }

      [Fact]
      public void Find_NonGeneric_HappyPathTest() {
         var a = new DummyImplementationA();
         var b = new DummyImplementationB();
         var instances = ImmutableDictionary.Of(
            typeof(DummyInterface), a,
            typeof(DummyImplementationA), a,
            typeof(DummyImplementationB), b,
            typeof(object), new object());
         var instancesByTypeLinq = (SCG.IEnumerable<SCG.KeyValuePair<Type, object>>)instancesByType;

         When(instancesByTypeLinq.GetEnumerator()).ThenReturn(instances.GetEnumerator());

         var matches = testObj.Find(typeof(DummyInterface)).ToList();

         Verify(instancesByTypeLinq).GetEnumerator();
         VerifyNoMoreInteractions();

         AssertEquals(2, matches.Count);
         AssertTrue(matches.Contains(a));
         AssertTrue(matches.Contains(b));
      }

      public interface DummyInterface { }

      private class DummyImplementationA : DummyInterface {}
      private class DummyImplementationB : DummyInterface {}
   }
}
