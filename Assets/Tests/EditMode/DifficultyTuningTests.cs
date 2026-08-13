using NUnit.Framework;

public class DifficultyTuningTests
{
    [Test]
    public void DeriveGlobal_AtDifficultyZero_ProducesLowerBoundValues()
    {
        var tuning = new DifficultyTuning();
        tuning.DeriveGlobal(0f, levelIndex: 0);

        Assert.AreEqual(0f, tuning.difficulty, 0.0001f);
        Assert.AreEqual(0.85f, tuning.playerSpeedMultiplier, 0.01f);
        Assert.AreEqual(1.25f, tuning.swarmCohesionMultiplier, 0.01f);
        Assert.AreEqual(6f, tuning.coinObstacleClearance, 0.01f);
    }

    [Test]
    public void DeriveGlobal_AtDifficultyOne_ProducesUpperBoundValues()
    {
        var tuning = new DifficultyTuning();
        tuning.DeriveGlobal(1f, levelIndex: 0);

        Assert.AreEqual(1f, tuning.difficulty, 0.0001f);
        Assert.AreEqual(1.35f, tuning.playerSpeedMultiplier, 0.01f);
        Assert.AreEqual(0.8f, tuning.swarmCohesionMultiplier, 0.01f);
        Assert.AreEqual(1f, tuning.coinObstacleClearance, 0.01f);
    }

    [Test]
    public void DeriveGlobal_ClampsInputAboveOne()
    {
        var tuning = new DifficultyTuning();
        tuning.DeriveGlobal(5f, levelIndex: 0);

        Assert.AreEqual(1f, tuning.difficulty, 0.0001f);
    }

    [Test]
    public void DeriveGlobal_ClampsInputBelowZero()
    {
        var tuning = new DifficultyTuning();
        tuning.DeriveGlobal(-3f, levelIndex: 0);

        Assert.AreEqual(0f, tuning.difficulty, 0.0001f);
    }

    [Test]
    public void DeriveHazard_ObstacleAtDifficultyOne_MaxesOutDamageAndSpawnMultipliers()
    {
        var tuning = new DifficultyTuning();
        tuning.DeriveHazard(HazardType.Obstacle, 1f, levelIndex: 0);

        Assert.AreEqual(1f, tuning.obstacleDifficulty, 0.0001f);
        Assert.AreEqual(1.5f, tuning.obstacleDamageMultiplier, 0.01f);
        Assert.AreEqual(1.35f, tuning.obstacleSpawnMultiplier, 0.01f);
    }

    [Test]
    public void DeriveHazard_EachHazardTypeIsIndependent()
    {
        var tuning = new DifficultyTuning();
        tuning.DeriveHazard(HazardType.Laser, 1f, levelIndex: 0);
        tuning.DeriveHazard(HazardType.BlackHole, 0f, levelIndex: 0);

        Assert.AreEqual(1f, tuning.GetHazardDifficulty(HazardType.Laser), 0.0001f);
        Assert.AreEqual(0f, tuning.GetHazardDifficulty(HazardType.BlackHole), 0.0001f);
        Assert.Greater(tuning.laserDamageMultiplier, tuning.blackHoleDamageMultiplier);
    }

    [Test]
    public void DeriveHazard_AtHighLevelIndex_EscalatesDamageBeyondBaseline()
    {
        // Regressionsschutz für die "ab Level 10 wird's brutal"-Eskalation.
        var tuningBaseline = new DifficultyTuning();
        tuningBaseline.DeriveHazard(HazardType.Obstacle, 1f, levelIndex: 0);

        var tuningDeepLevel = new DifficultyTuning();
        tuningDeepLevel.DeriveHazard(HazardType.Obstacle, 1f, levelIndex: 14); // Level 15

        Assert.Greater(tuningDeepLevel.obstacleDamageMultiplier, tuningBaseline.obstacleDamageMultiplier);
    }

    [Test]
    public void DeriveGlobal_AtExtremeLevelIndex_NeverProducesNegativeMultiplier()
    {
        // Regressionstest: bei sehr hoher Intensität (Endlos-Modus, unbegrenzte Distanz) konnte
        // swarmCohesionMultiplier vor dem Fix negativ werden und den Schwarm zerstören.
        var tuning = new DifficultyTuning();
        tuning.DeriveGlobal(1f, levelIndex: 500);

        Assert.Greater(tuning.swarmCohesionMultiplier, 0f);
        Assert.Greater(tuning.playerSpeedMultiplier, 0f);
    }
}
