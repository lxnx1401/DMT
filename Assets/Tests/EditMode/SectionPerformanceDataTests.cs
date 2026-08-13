using NUnit.Framework;

public class SectionPerformanceDataTests
{
    [Test]
    public void TimeStruggle_WhenUnderTargetTime_ReturnsZero()
    {
        var data = new SectionPerformanceData { sectionTime = 4f, targetTime = 8f };
        Assert.AreEqual(0f, data.TimeStruggle, 0.0001f);
    }

    [Test]
    public void TimeStruggle_WhenDoubleTargetTime_ReturnsOne()
    {
        var data = new SectionPerformanceData { sectionTime = 16f, targetTime = 8f };
        Assert.AreEqual(1f, data.TimeStruggle, 0.0001f);
    }

    [Test]
    public void TimeStruggle_WithZeroTargetTime_ReturnsZeroInsteadOfDividingByZero()
    {
        var data = new SectionPerformanceData { sectionTime = 5f, targetTime = 0f };
        Assert.AreEqual(0f, data.TimeStruggle, 0.0001f);
    }

    [Test]
    public void ParticleLossStruggle_NoLoss_ReturnsZero()
    {
        var data = new SectionPerformanceData { particlesAtStart = 1000, particlesLost = 0 };
        Assert.AreEqual(0f, data.ParticleLossStruggle, 0.0001f);
    }

    [Test]
    public void ParticleLossStruggle_QuarterLost_ReturnsOne()
    {
        // Formel ist lost/start * 4, geclampt auf 1 - bei 25% Verlust schon maximaler Struggle.
        var data = new SectionPerformanceData { particlesAtStart = 1000, particlesLost = 250 };
        Assert.AreEqual(1f, data.ParticleLossStruggle, 0.0001f);
    }

    [Test]
    public void ParticleLossStruggle_WithZeroParticlesAtStart_ReturnsZero()
    {
        var data = new SectionPerformanceData { particlesAtStart = 0, particlesLost = 0 };
        Assert.AreEqual(0f, data.ParticleLossStruggle, 0.0001f);
    }

    [Test]
    public void CollisionStruggle_ThreeHits_ReturnsOne()
    {
        var data = new SectionPerformanceData();
        data.hitsByType[HazardType.Obstacle] = 3;

        Assert.AreEqual(1f, data.CollisionStruggle, 0.0001f);
    }

    [Test]
    public void CollisionStruggle_SumsAcrossHazardTypes()
    {
        var data = new SectionPerformanceData();
        data.hitsByType[HazardType.Laser] = 1;
        data.hitsByType[HazardType.BlackHole] = 2;

        Assert.AreEqual(3, data.TotalHits);
        Assert.AreEqual(1f, data.CollisionStruggle, 0.0001f);
    }

    [Test]
    public void GetHazardStruggle_OnlyCountsTheRequestedType()
    {
        var data = new SectionPerformanceData();
        data.hitsByType[HazardType.Laser] = 3;
        data.hitsByType[HazardType.BlackHole] = 0;

        Assert.AreEqual(1f, data.GetHazardStruggle(HazardType.Laser), 0.0001f);
        Assert.AreEqual(0f, data.GetHazardStruggle(HazardType.BlackHole), 0.0001f);
    }

    [Test]
    public void OverallStruggleScore_CombinesWeightedComponents()
    {
        // TimeStruggle=1 (0.25), ParticleLossStruggle=1 (0.5), CollisionStruggle=1 (0.25) -> 1.0
        var data = new SectionPerformanceData
        {
            sectionTime = 20f,
            targetTime = 8f,
            particlesAtStart = 1000,
            particlesLost = 1000
        };
        data.hitsByType[HazardType.Obstacle] = 5;

        Assert.AreEqual(1f, data.OverallStruggleScore, 0.0001f);
    }

    [Test]
    public void OverallStruggleScore_WithNoStruggleAtAll_ReturnsZero()
    {
        var data = new SectionPerformanceData
        {
            sectionTime = 4f,
            targetTime = 8f,
            particlesAtStart = 1000,
            particlesLost = 0
        };

        Assert.AreEqual(0f, data.OverallStruggleScore, 0.0001f);
    }
}
