# Vox2

Goal: Rewrite Vox to have lower overhead and fewer polymorphic read/writes.
Polymorphic read/writes entail a lookup which is costly, especially if paid per-element.

## Source Generation

Type read/write methods are emitted via source generators, essentially performing per-member read/writes within a method.
These member read/writes are calls to further generated functions.

* e.g. ReadIntPoint3 invokes ReadInt32

## Round-Trip Nuances

Custom Objects can be written 'full' with a type, or 'raw' without a type. 

* A Custom Object written 'full' should round-trip if read via ReadPolymorphic, assuming the object is registered in VoxTypes.

Collections (List, Dict, etc) have specialized serializers; unlike the original Vox, they are not serialized as "listlike" or "dictlike"
types (and thus unserializable as anything else); if you fully serialize a HashSet vs a List, the specialized type info will be emitted.

## Multi-Context Nuances

A single source generation occurs per Vox type; Vox 1 supports potentially numerous serializers per type, each behaving differently
(e.g. in one context an Int might be var-int serializsed, whereas in another it might be fixed-byte serialized). In Vox2, this is not
permitted; at runtime, a type may only have one form of serialization.

Likewise, Vox1 aimed to support scoped serializable types; for example, if a Vox object of type A were not included in a vox context,
it could not be serialized by a known type B of that vox context. In contrast, Vox2 performs no such scoping; type B's serialization
Write() will directly invoke type A's Write(), where the typing is known prior. In fact, the Write() is even permitted to be inlined.

Still, Vox2 permits scoping a VoxContext to a subset of types via the context's constructor-time VoxTypes object. This scoping only
applies for polymorphic reads/writes; the scope-check is performed by the type trie.

Direct read/writes, as mentioned above, bypass such scope-checks and can be inlined. If a type B<A> invokes A's serializer, this
invocation is assumed to be direct (even though in practice it will likely be indirect), so no scope check occurs.

### Caching

An interesting situation occurs if a serializer for type B<A> dynamically looks up the serializer for type A in one context, then is 
reinvoked for another context. There are two cases, both of which respect scoping rules:

Case 1: Serializer of B<A> directly invokes Serializer of A. Direct invocations are not protected from context scoping, so this is OK.

Case 2: Serializer of B<A> looks up and caches the serializer for A. Context scoping works so long as Serializer B<A> can only be reached
via the type trie. In the case of a direct invocation to serializer B<A>, which is undefined behavior, we assume scoping rules do not apply
to B<A> and B<A> serializer invocations to the serializer of A are assumed OK.

## Custom Vox Types

The vast majority of Vox types can be auto-serialized via [VoxType(typeId)]. This generates an 'Auto-Serialized' type attribute
VoxInternalsAutoSerializedTypeInfoAttribute with property GenericSerializerTypeDefinition denoting the serializer type.

## Generic Types

Generic Types are interesting as their serialization/deserialization must handle multi-context scoping. Such scoping is handled by
type reading/writing (the type trie); not the serializers themselves. For this reason, serializers may be global singletons.

Because of the Multi-Context nuances above, it's 100% OK for an auto-serialized generic type to cache its generic serializer. Direct
invocations of read/write may hit this serializer and bypass scope-checks. Indirect polymorphic serialize/deserializes would hit the
type trie (performing the scope-check) prior to touching the generic serializer.
