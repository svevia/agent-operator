﻿using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection.Agents
{
    public class PhpInjectionTests : IClassFixture<TestingContext>
    {
        private const string ScenarioName = "injection-php";

        private readonly TestingContext _context;

        public PhpInjectionTests(TestingContext context, ITestOutputHelper outputHelper)
        {
            _context = context;
            _context.RegisterOutput(outputHelper);
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_agent_injection_environment_variables()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>(ScenarioName);

            // Assert
            using (new AssertionScope())
            {
                var container = result.Spec.Containers.Should().ContainSingle().Subject;

                container.Env.Should().Contain(x => x.Name == "PHP_INI_SCAN_DIR")
                         .Which.Value.Should().Be(":/usr/local/lib/contrast/php/ini/");
            }
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_agent_injection_volume_mount()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>(ScenarioName);

            // Assert
            result.Spec.Containers.Should().ContainSingle()
                  .Which.VolumeMounts.Should().ContainSingle(x => x.Name == "contrast")
                  .Which.MountPath.Should().Be("/usr/local/lib/contrast/php");
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_agent_injection_init_image()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>(ScenarioName);

            // Assert
            result.Spec.InitContainers.Should().ContainSingle(x => x.Name == "contrast-init")
                  .Which.Image.Should().Be("contrast/agent-php:latest");
        }
    }
}