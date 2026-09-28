namespace OSCU.Api.IntegrationTests;

/// <summary>
/// The JSON envelope every successful endpoint returns, declared here rather
/// than reusing <c>Result&lt;T&gt;</c>.
/// </summary>
/// <remarks>
/// Two reasons. Result&lt;T&gt; has only a private constructor, so
/// System.Text.Json cannot rehydrate it. More importantly, an integration test
/// should assert against the wire contract the React client actually consumes:
/// if someone renames a property on Result&lt;T&gt;, that is a breaking API
/// change and this test should fail rather than quietly follow along.
/// </remarks>
public record ApiEnvelope<T>(bool IsSuccess, T? Data);
