﻿// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using k8s.Models;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection;

public class RestrictedPolicyTests : IClassFixture<TestingContext>
{
    private const string ScenarioName = "injection-restricted";

    private readonly TestingContext _context;

    public RestrictedPolicyTests(TestingContext context, ITestOutputHelper outputHelper)
    {
        _context = context;
        _context.RegisterOutput(outputHelper);
    }

    [Fact]
    public async Task When_injected_then_pod_should_have_injection_annotations()
    {
        // The testing-restricted namespace enforces the restrictive policy.
        // This test is just a sanity check around if our patches will allow the pod to be deployed.

        var client = await _context.GetClient(defaultNamespace: "testing-restricted");

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        result.Annotations().Should().ContainKey("agents.contrastsecurity.com/is-injected").WhoseValue.Should().Be("True");
    }
}
