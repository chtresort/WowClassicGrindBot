﻿using Microsoft.Extensions.Logging;

namespace Core.Goals;

public sealed class ConditionalWaitGoal : GoapGoal
{
    public override float Cost => Keys[0].Cost;

    private readonly ILogger logger;
    private readonly Wait wait;

    public ConditionalWaitGoal(KeyAction keyAction, ILogger logger, Wait wait)
        : base(nameof(ConditionalWaitGoal))
    {
        this.logger = logger;
        this.wait = wait;

        Keys = new KeyAction[1] { keyAction };
    }

    public override bool CanRun() => Keys[0].CanRun();

    public override void OnEnter()
    {
        logger.LogInformation($"Waiting for {Keys[0].Name}");
    }

    public override void Update()
    {
        wait.Update();
    }
}