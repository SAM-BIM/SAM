# SAM.Tests

Round-trip regression tests for SAM JSON serialization. The point of these tests
is to assert that for any SAM object: serialize → deserialize → serialize
produces structurally identical JSON. They catch silent format regressions
during the Newtonsoft.Json → System.Text.Json migration and beyond.

## Run

```
cd SAM\SAM.Tests
dotnet test
```

## What's covered

- `ParameterSetTests` — one test per `JTokenType` branch in `ParameterSet.FromJObject` (string, integer, fractional double, whole-number double, bool, DateTime, Guid, Color), plus heuristic edge cases (strings that look like dates).
- `SAMObjectTests` — Guid/Name preservation; two-pass idempotency.
- `LibraryFixtureTests` — every `SAM_*Library.JSON` fixture in `files/resources/Analytical/` is loaded, round-tripped, and structurally compared.

## How comparison works

`Helpers/JsonEquivalence.cs` parses both JSON strings with `System.Text.Json.JsonDocument`
and walks the trees:

- Objects compared **order-insensitive** on keys.
- Arrays compared **order-sensitive**.
- Numbers compared by value (`5` == `5.0`); whitespace and formatting ignored.

No reference to any `JObject` type is made anywhere in the test code, so the
same tests run unchanged against pre-migration Newtonsoft code and against the
System.Text.Json shim.

## How to add a test

For any new SAM type with `(JObject)` ctor and `ToJObject()`:

```csharp
[Fact]
public void RoundTrip_MyType()
{
    MyType input = ...build sample...;
    MyType result = RoundTrip.Once(input);
    Assert.Equal(input.SomeField, result.SomeField);
}
```

For a fixture file:

```csharp
[Fact]
public void MyFixture_RoundTrip()
{
    string json = Fixtures.ReadAllText("SAM_MyFixture.JSON");
    MyType value = RoundTrip.FromJson<MyType>(json);
    Assert.NotNull(value);
}
```

## Not in SAM.sln

The csproj is standalone — add it to `SAM.sln` when you want it built as part
of the main solution. Run from the test folder directly until then.
