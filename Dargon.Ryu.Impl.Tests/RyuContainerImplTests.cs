using System;
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
         var dummy = CreateMock<DummyType>();
         RyuPackageV1TypeInfo typeInfo;
         When(typeInfosByType.TryGetValue(typeof(DummyType), out typeInfo)).ThenReturn(false);
         testObj.Set<DummyType>(dummy);
         Verify(typeInfosByType).TryGetValue(typeof(DummyType), out typeInfo);
         Verify(instancesByType)[typeof(DummyType)] = dummy;
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void Set_CachedType_Test() {
         var dummy = CreateMock<DummyType>();
         var typeInfo = CreateMock<RyuPackageV1TypeInfo>();
         var typeInfoPlaceHolder = CreateMock<RyuPackageV1TypeInfo>();
         When(typeInfo.Flags).ThenReturn(RyuTypeFlags.Cache);
         When(typeInfosByType.TryGetValue(typeof(DummyType), out typeInfoPlaceHolder)).Set(typeInfoPlaceHolder, typeInfo).ThenReturn(true);
         testObj.Set<DummyType>(dummy);
         Verify(typeInfo).Flags.Wrap();
         Verify(typeInfosByType).TryGetValue(typeof(DummyType), out typeInfo);
         Verify(instancesByType)[typeof(DummyType)] = dummy;
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void Set_NonCachedType_Test() {
         var dummy = CreateMock<DummyType>();
         var typeInfo = CreateMock<RyuPackageV1TypeInfo>();
         var typeInfoPlaceHolder = CreateMock<RyuPackageV1TypeInfo>();
         When(typeInfo.Flags).ThenReturn(RyuTypeFlags.None);
         When(typeInfosByType.TryGetValue(typeof(DummyType), out typeInfoPlaceHolder)).Set(typeInfoPlaceHolder, typeInfo).ThenReturn(true);
         AssertThrows<InvalidOperationException>(() => testObj.Set(dummy));
         Verify(typeInfo).Flags.Wrap();
         Verify(typeInfosByType).TryGetValue(typeof(DummyType), out typeInfo);
         VerifyNoMoreInteractions();
      }

      public interface DummyType { }
   }
}
