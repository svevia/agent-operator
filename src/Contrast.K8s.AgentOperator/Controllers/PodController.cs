﻿using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core;
using Contrast.K8s.AgentOperator.Core.Events;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Operator.Rbac;
using KubeOps.Operator.Webhooks;
using MediatR;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [EntityRbac(typeof(V1Pod), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
    public class PodController : IMutationWebhook<V1Pod>
    {
        private readonly IMediator _mediator;

        public AdmissionOperations Operations => AdmissionOperations.Create;

        public PodController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<MutationResult> CreateAsync(V1Pod newEntity, bool dryRun)
        {
            var result = await _mediator.Send(new EntityCreating<V1Pod>(newEntity));
            return result is NeedsChangeEntityCreatingMutationResult<V1Pod>
                ? MutationResult.Modified(result)
                : MutationResult.NoChanges();
        }
    }
}
