using System.Collections.Generic;
using UnityEngine;

namespace SmartData.FindFake
{
    /// <summary>Izbor display profila i računanje rasporeda zona igrača.</summary>
    public static class LayoutResolver
    {
        public static int SelectProfileIndex(List<DisplayProfile> profiles, Vector2 screenSize)
        {
            if (profiles == null || profiles.Count == 0) return -1;
            if (screenSize.x <= 0f || screenSize.y <= 0f) return 0;

            bool portrait = screenSize.y > screenSize.x;
            float aspect = screenSize.x / screenSize.y;
            int best = 0;
            float bestScore = float.MaxValue;
            for (int i = 0; i < profiles.Count; i++)
            {
                DisplayProfile p = profiles[i];
                if (p == null) continue;
                float score = Mathf.Abs(Mathf.Log(p.Aspect / aspect));
                if (p.IsPortrait != portrait) score += 10f;
                // Kod istog aspekta prednost ima profil najbliže rezolucije.
                score += Mathf.Abs(p.resolution.x - screenSize.x) / 100000f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best;
        }

        public static ResolvedLayout Resolve(DisplayProfile profile, LayoutBlock layout)
        {
            bool overrideLayout = profile != null && profile.overrideLayout;
            bool portrait = profile != null && profile.IsPortrait;

            SplitMode split = overrideLayout ? profile.split : layout.split;
            if (split == SplitMode.Auto)
            {
                SplitDirection direction = overrideLayout && profile.splitDirection != SplitDirection.Auto
                    ? profile.splitDirection
                    : layout.splitDirection;
                split = SplitFor(direction, layout.seating, portrait);
            }

            var r = new ResolvedLayout
            {
                split = split,
                player1First = !layout.swapPlayers,
                centerGap = overrideLayout ? profile.centerGap : layout.centerGap,
                hudSize = overrideLayout ? profile.hudSize : layout.hudSize,
                hudPlacement = overrideLayout ? profile.hudPlacement : layout.hudPlacement
            };

            if (overrideLayout && profile.overrideRotations)
            {
                r.player1Rotation = profile.player1Rotation;
                r.player2Rotation = profile.player2Rotation;
                return r;
            }

            ZoneRotation first = ZoneRotation.Deg0;
            ZoneRotation second = ZoneRotation.Deg0;
            if (layout.seating == SeatingMode.TableOpposite)
            {
                if (split == SplitMode.TopBottom)
                {
                    first = ZoneRotation.Deg0;     // igrač stoji uz donju ivicu
                    second = ZoneRotation.Deg180;  // igrač stoji uz gornju ivicu
                }
                else
                {
                    first = ZoneRotation.Deg270;   // igrač stoji uz levu ivicu
                    second = ZoneRotation.Deg90;   // igrač stoji uz desnu ivicu
                }
            }

            r.player1Rotation = r.player1First ? first : second;
            r.player2Rotation = r.player1First ? second : first;
            return r;
        }

        /// <summary>
        /// Pretvara smer podele (duža/kraća ivica) u konkretnu podelu za datu orijentaciju.
        /// Landscape: duže ivice su gore/dole → TopBottom. Portrait: duže ivice su levo/desno → LeftRight.
        /// </summary>
        public static SplitMode SplitFor(SplitDirection direction, SeatingMode seating, bool portrait)
        {
            if (direction == SplitDirection.Auto)
            {
                // Zadržava ponašanje iz v1.0: sto u landscape-u → duže ivice; sve ostalo → kraće ivice.
                direction = seating == SeatingMode.TableOpposite && !portrait
                    ? SplitDirection.AlongLongSide
                    : SplitDirection.AlongShortSide;
            }

            bool longSides = direction == SplitDirection.AlongLongSide;
            if (portrait) return longSides ? SplitMode.LeftRight : SplitMode.TopBottom;
            return longSides ? SplitMode.TopBottom : SplitMode.LeftRight;
        }

        public static void ConfigureHalf(PlayerHalf half, ResolvedLayout r, LayoutBlock layout)
        {
            if (half == null) return;
            bool isPlayer1 = half.player != 2;
            bool first = isPlayer1 == r.player1First;
            ZoneRotation rotation = isPlayer1 ? r.player1Rotation : r.player2Rotation;
            float g = Mathf.Clamp01(r.centerGap) * 0.5f;

            Vector2 min, max;
            if (r.split == SplitMode.LeftRight)
            {
                min = first ? new Vector2(0f, 0f) : new Vector2(0.5f + g, 0f);
                max = first ? new Vector2(0.5f - g, 1f) : new Vector2(1f, 1f);
            }
            else
            {
                min = first ? new Vector2(0f, 0f) : new Vector2(0f, 0.5f + g);
                max = first ? new Vector2(1f, 0.5f - g) : new Vector2(1f, 1f);
            }
            half.Configure(min, max, rotation, layout.halfPadding);
        }
    }
}
