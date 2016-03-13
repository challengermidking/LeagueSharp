using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using ItemData = LeagueSharp.Common.Data.ItemData;


namespace Sense_Katarina
{
    class Program
    {
        private static Menu Option;
        private static Spell Q, W, E, R;
        private static Obj_AI_Hero Player;
        private static Orbwalking.Orbwalker orbWalker;
        private static string championName = "Katarina";
        private static SpellSlot Ignite = ObjectManager.Player.GetSpellSlot("summonerDot");

        public static HpBarIndicator Indicator = new HpBarIndicator();

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.ChampionName != championName) return;

            Q = new Spell(SpellSlot.Q, 675f);
            W = new Spell(SpellSlot.W, 375f);
            E = new Spell(SpellSlot.E, 700f);
            R = new Spell(SpellSlot.R, 550f);

            MainMenu();
            Game.OnUpdate += OnUpate;
        }


        static void OnUpate(EventArgs args)
        {
            if (Player.IsDead) return;

            switch (orbWalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.LastHit:
                    LastHit();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
            }
        }

        static void LastHit()
        {
            if (Option_item("Last Q") && Q.IsReady())
            {
                var Minion = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly)
     .Where(x => x.Health <= Q.GetDamage(x)).OrderByDescending(x => x.MaxHealth).ThenByDescending(x => x.Distance(Player)).FirstOrDefault();

                if (Minion != null)
                    Q.CastOnUnit(Minion, true);
            }
        }

        static void Harass()
        {
            if (Option_item("Harass Q") && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (target != null)
                    Q.CastOnUnit(target, true);
            }

            if (Option_item("Harass W") && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
                if (target != null)
                    W.Cast(true);
            }
        }

        static void LaneClear()
        {
            var Minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            if (Minions.Count > 0)
            {
                foreach (var MInion in Minions)
                {
                    if (Option_item("Lane Q") && Q.IsReady())
                    {
                        if (MInion.Distance(Player.ServerPosition) >= W.Range)
                            Q.CastOnUnit(MInion, true);

                        if (Option_item("Lane W") && W.IsReady() && MInion.Distance(Player.ServerPosition) <= W.Range)
                        {
                            var Last_QW_WMinion = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly)
.Where(x => x.Health <= (W.GetDamage(x) + Q.GetDamage(x))).OrderByDescending(x => x.MaxHealth).ThenByDescending(x => x.Distance(Player)).FirstOrDefault();
                            Q.CastOnUnit(Last_QW_WMinion, true);
                            W.Cast(true);
                        }
                    }

                    if (Option_item("Lane W") && W.IsReady())
                    {
                        var Last_W_Minion = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly)
.Where(x => x.Health <= W.GetDamage(x)).OrderByDescending(x => x.MaxHealth).ThenByDescending(x => x.Distance(Player)).FirstOrDefault();
                        MinionManager.FarmLocation farmLocation = W.GetLineFarmLocation(Minions);

                        if (Last_W_Minion != null)
                            W.Cast(true);

                        if (farmLocation.Position.IsValid())
                        {
                            if (farmLocation.MinionsHit >= 3)
                                W.Cast(true);
                        }
                    }

                    if (Option_item("Lane E") && E.IsReady())
                    {
                        var Last_E_Minion = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly)
.Where(x => x.Health <= E.GetDamage(x)).OrderByDescending(x => x.MaxHealth).ThenByDescending(x => x.Distance(Player)).FirstOrDefault();
                        if (Last_E_Minion != null)
                            E.CastOnUnit(Last_E_Minion, true);
                    }
                }
            }
            if (Minions.Count == 0) return;
        }

        static void JungleClear()
        {
            var JungleMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            MinionManager.FarmLocation Mob = W.GetLineFarmLocation(JungleMinions);
            if (JungleMinions.Count > 0)
            {
                foreach (var Mobs in JungleMinions)
                {
                    if (Option_item("Jungle Q") && Q.IsReady() && Option_item("Jungle W") && W.IsReady())
                    {
                            if (Mob.Position.IsValid())
                                if (Mob.MinionsHit >= JungleMinions.Count)
                                {
                                    Q.CastOnUnit(Mobs, true);
                                    W.Cast(true);
                                }
                    }

                    if (Option_item("Jungle W") && W.IsReady())
                    {
                        if (JungleMinions.Count >= Mob.MinionsHit)
                            W.Cast(true);
                    }

                    if (Option_item("Jungle E") && E.IsReady())
                    {
                        var Last_E_Mob = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly)
.Where(x => x.Health < E.GetDamage(x)).OrderByDescending(x => x.MaxHealth).ThenByDescending(x => x.Distance(Player)).FirstOrDefault();
                        if (Last_E_Mob != null)
                            E.CastOnUnit(Last_E_Mob, true);
                    }
                }
            }
        }

        static void Combo()
        {

        }

        static bool Option_item(string itemname)
        {
            return Option.Item(itemname).GetValue<bool>();
        }

        static float GetComboDamage(Obj_AI_Hero Enemy)
        {
            float damage = 0;

            if (Q.IsReady())
                damage += Q.GetDamage(Enemy);

            if (W.IsReady())
                damage += W.GetDamage(Enemy);

            if (E.IsReady())
                damage += E.GetDamage(Enemy);

            if (R.IsReady())
                damage += R.GetDamage(Enemy);

            if (Player.Spellbook.CanUseSpell(Player.GetSpellSlot("summonerdot")) == SpellState.Ready)
                damage += (float)Player.GetSummonerSpellDamage(Enemy, Damage.SummonerSpell.Ignite);

            if (!Player.IsWindingUp)
                damage += (float)ObjectManager.Player.GetAutoAttackDamage(Enemy, true);

            return damage;
        }

        public static void Drawing_OnEndScene(EventArgs args)
        {
            if (Player.IsDead) return;
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(ene => ene.IsValidTarget() && !ene.IsZombie))
            {
                if (Option_item("DamageAfterCombo"))
                {
                    Indicator.unit = enemy;
                    Indicator.drawDmg(GetComboDamage(enemy), new ColorBGRA(255, 204, 0, 160));
                }
            }
        }

        static void MainMenu()
        {

            Option = new Menu("Sense Katarina", "Sense_Katarina", true).SetFontStyle(System.Drawing.FontStyle.Regular, SharpDX.Color.SkyBlue);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Option.AddSubMenu(targetSelectorMenu);

            Option.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            orbWalker = new Orbwalking.Orbwalker(Option.SubMenu("Orbwalker"));

            var LastHit = new Menu("Last Hit", "Last Hit");
            {
                LastHit.AddItem(new MenuItem("Last Use Q", "Use Q").SetValue(true));
            }
            Option.AddSubMenu(LastHit);

            var Harass = new Menu("Harass", "Harass");
            {
                Harass.AddItem(new MenuItem("Harass Q", "Use Q").SetValue(true));
                Harass.AddItem(new MenuItem("Harass W", "Use W").SetValue(true));
            }
            Option.AddSubMenu(Harass);

            var LaneC = new Menu("Lane Clear", "Lane Clear");
            {
                LaneC.AddItem(new MenuItem("Lane Q", "Use Q").SetValue(true));
                LaneC.AddItem(new MenuItem("Lane W", "Use W").SetValue(true));
                LaneC.AddItem(new MenuItem("Lane E", "Use E").SetValue(false));
            }
            Option.AddSubMenu(LaneC);

            var JungleC = new Menu("Jungle Clear", "Jungle Clear");
            {
                JungleC.AddItem(new MenuItem("Jungle Q", "Use Q").SetValue(true));
                JungleC.AddItem(new MenuItem("Jungle W", "Use W").SetValue(true));
                JungleC.AddItem(new MenuItem("Jungle E", "Use E").SetValue(false));
            }
            Option.AddSubMenu(JungleC);

            var Combo = new Menu("Combo", "Combo");
            {
                Combo.AddItem(new MenuItem("Combo Q", "Use Q").SetValue(true));
                Combo.AddItem(new MenuItem("Combo W", "Use W").SetValue(true));
                Combo.AddItem(new MenuItem("Combo E", "Use E").SetValue(true));
                Combo.AddItem(new MenuItem("Combo R", "Use R").SetValue(true));
                Combo.AddItem(new MenuItem("Combo Ignite", "Use Ignite").SetValue(true));
            }
            Option.AddSubMenu(Combo);

            var Draw = new Menu("Drawing", "Drawing");
            {
                Draw.AddItem(new MenuItem("Draw Q", "Use Q").SetValue(false));
                Draw.AddItem(new MenuItem("Draw W", "Use W").SetValue(false));
                Draw.AddItem(new MenuItem("Draw E", "Use E").SetValue(false));
                Draw.AddItem(new MenuItem("Draw R", "Use R").SetValue(false));
                Draw.AddItem(new MenuItem("DamageAfterCombo", "Draw Combo Damage").SetValue(true));
            }
            Option.AddSubMenu(Draw);
        }
    }
}
