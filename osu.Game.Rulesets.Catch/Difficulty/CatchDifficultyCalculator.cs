// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Difficulty.Skills;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Catch.Difficulty
{
    public class CatchDifficultyCalculator : DifficultyCalculator
    {
        private const double star_scaling_factor = 0.145;

        protected override int SectionLength => 750;

        private readonly float halfCatchWidth;

        public CatchDifficultyCalculator(Ruleset ruleset, WorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
            var catcher = new CatcherArea.Catcher(beatmap.BeatmapInfo.BaseDifficulty);
            halfCatchWidth = catcher.CatchWidth * 0.5f;
        }

        protected override void PopulateAttributes(DifficultyAttributes attributes, IBeatmap beatmap, Skill[] skills, double timeRate)
        {
            var catchAttributes = (CatchDifficultyAttributes)attributes;

            // this is the same as osu!, so there's potential to share the implementation... maybe
            double preempt = BeatmapDifficulty.DifficultyRange(beatmap.BeatmapInfo.BaseDifficulty.ApproachRate, 1800, 1200, 450) / timeRate;

            catchAttributes.StarRating = Math.Sqrt(skills[0].DifficultyValue()) * star_scaling_factor;
            catchAttributes.ApproachRate = preempt > 1200.0 ? -(preempt - 1800.0) / 120.0 : -(preempt - 1200.0) / 150.0 + 5.0;
            catchAttributes.MaxCombo = beatmap.HitObjects.Count(h => h is Fruit) + beatmap.HitObjects.OfType<JuiceStream>().SelectMany(j => j.NestedHitObjects).Count(h => !(h is TinyDroplet));
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, double timeRate)
        {
            CatchHitObject lastObject = null;

            foreach (var hitObject in beatmap.HitObjects.OfType<CatchHitObject>())
            {
                if (lastObject == null)
                {
                    lastObject = hitObject;
                    continue;
                }

                switch (hitObject)
                {
                    // We want to only consider fruits that contribute to the combo. Droplets are addressed as accuracy and spinners are not relevant for "skill" calculations.
                    case Fruit fruit:
                        yield return new CatchDifficultyHitObject(fruit, lastObject, timeRate, halfCatchWidth);
                        break;
                    case JuiceStream _:
                        foreach (var nested in hitObject.NestedHitObjects.OfType<CatchHitObject>().Where(o => !(o is TinyDroplet)))
                            yield return new CatchDifficultyHitObject(nested, lastObject, timeRate, halfCatchWidth);
                        break;
                }

                lastObject = hitObject;
            }
        }

        protected override Skill[] CreateSkills() => new Skill[]
        {
            new Movement(),
        };

        protected override DifficultyAttributes CreateDifficultyAttributes(Mod[] mods) => new CatchDifficultyAttributes(mods);
    }
}
