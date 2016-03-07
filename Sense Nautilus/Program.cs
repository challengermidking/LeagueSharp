using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using SharpDX;

namespace Sense_Nautilus
{
    class Program
    {

        private static Menu Option;
        private static Obj_AI_Hero Player;
        private static Orbwalking.Orbwalker orbWalker;
        private static string championName = "Nautilus";
        private static Spell Q, W, E, R;
        public static HpBarIndicator Indicator = new HpBarIndicator();

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.ChampionName != championName) return;

            Q = new Spell(SpellSlot.Q, 1100f);
            W = new Spell(SpellSlot.W, 175f);
            E = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R, 825f);

            Q.SetSkillshot(250, 90, 2000, true, SkillshotType.SkillshotLine);

            MainMenu();
            Game.OnUpdate += OnUpate;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
        }

        static void OnUpate(EventArgs args)
        {
            if (Player.IsDead) return;

            Flee();
            KillSteal();


            switch (orbWalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    JungleC();
                    break;
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
            }
        }

        static void OnInterruptableTarget(Obj_AI_Hero Sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Option_Item("Interrupter Q") && Q.IsReady())
                if (ObjectManager.Player.Distance(Sender) < Q.Range && args.DangerLevel >= Interrupter2.DangerLevel.Medium && Q.GetPrediction(Sender).Hitchance >= HitChance.Medium)
                    Q.Cast(Sender);

            if (Option_Item("Interrupter R") && R.IsReady())
                if (ObjectManager.Player.Distance(Sender) < R.Range && args.DangerLevel == Interrupter2.DangerLevel.High)
                    R.Cast(Sender);
        }

        static void Harass()
        {
            if (Option_Item("Harass Q") && Q.IsReady())
                CastQ();

            if (Option_Item("Harass E") && E.IsReady())
                CastE();
        }

        static void LaneClear()
        {
            if (Option.Item("LToggle").GetValue<KeyBind>().Active)
            {
                var Minions = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);
                if (Minions != null)
                {
                    if (Option_Item("Lane Q") && Q.IsReady())
                    {
                        var Minion = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                                                .Where(x => x.Health < Q.GetDamage(x)).OrderByDescending(x => x.MaxHealth).ThenByDescending(x => x.Distance(Player)).FirstOrDefault();
                        if (Minion != null)
                            Q.Cast(Minion, true);
                    }

                    if (Option_Item("Lane E") && E.IsReady())
                    {
                        MinionManager.FarmLocation farmLocaion = E.GetCircularFarmLocation(Minions);
                        if (farmLocaion.Position.IsValid())
                        {
                            if (Minions.Count >= 6)
                                if (farmLocaion.MinionsHit >= 4)
                                    E.Cast(Player, true);

                            if (Minions.Count <= 5 && Minions.Count >= 3)
                                if (farmLocaion.MinionsHit >= 3)
                                    E.Cast(Player, true);

                            if (Minions.Count <= 2)
                                E.Cast(Player, true);
                            
                            if (Minions.Count == 0) return;
                        }
                    }
                }
                if (Minions == null) return;
            }
        }

        static void JungleC()
        {
            var JungleMinions = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (JungleMinions.Count >= 1)
            {
                if (Option.Item("JToggle").GetValue<KeyBind>().Active)
                {
                    if (Option_Item("Jungle Q"))
                    {
                        var Mob = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral)
.Where(x => x.Health < W.GetDamage(x)).OrderByDescending(x => x.MaxHealth).ThenByDescending(x => x.Distance(Player)).FirstOrDefault();
                        if (Mob != null)
                            Q.Cast(Mob, true);
                    }

                    if (Option_Item("Jungle E"))
                    {
                        MinionManager.FarmLocation Mobs = E.GetCircularFarmLocation(JungleMinions);
                        if (Mobs.Position.IsValid())
                        {
                            if (JungleMinions.Count == 4)
                                if (Mobs.MinionsHit >= 3)
                                    E.Cast(Player, true);

                            if (JungleMinions.Count == 3)
                                if (Mobs.MinionsHit >= 2)
                                    E.Cast(Player, true);

                            if (JungleMinions.Count <= 2)
                                E.Cast(Player, true);

                            if (JungleMinions.Count == 0) return;
                        }
                    }
                }
            }
        }

        static void Combo()
        {

            if (Option_Item("Combo Q") && Q.IsReady())
                CastQ();

            if (Option_Item("Combo E") && E.IsReady())
                CastE();

            if (Option_Item("Combo R") && R.IsReady())
                CastR();
        }

        static void CastQ()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var prediction = Q.GetPrediction(target);
            prediction.CollisionObjects.Count(x => x.IsEnemy && !x.IsZombie && (prediction.Hitchance >= HitChance.High || prediction.Hitchance == HitChance.Immobile));

            if (target != null && (Player.Distance(target) > E.Range || !E.IsReady()))
            {
                if (target.CanMove && Player.Distance(target) < Q.Range * 0.95)
                    Q.Cast(prediction.CastPosition, true);

                if (!target.CanMove)
                    Q.Cast(prediction.CastPosition, true);
            }
                

        }

        static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe)
                if (W.IsReady())
                    if ((orbWalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && Option_Item("Harass W")) || (orbWalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Option_Item("Lane W")) && Option.Item("LToggle").GetValue<KeyBind>().Active ||
                        (orbWalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Option_Item("Jungle W")) || (orbWalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Option_Item("Combo W")))
                        if (target is Obj_AI_Hero || target is Obj_AI_Minion || target is Obj_AI_Turret)
                            W.Cast();
              
        }

        static void CastE()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (target != null)
            {
                if (target.CanMove && Player.Distance(target) < E.Range * 0.95)
                    E.Cast(target, true);

                if (!target.CanMove)
                    E.Cast(target, true);
            }
        }

        static void CastR()
        {
            var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
            if (target != null && R.IsReady())
            {
                if (target.Health < GetComboDamage(target))
                    R.CastOnUnit(target, true);

                if (!Q.IsReady() && !W.IsReady() && !E.IsReady())
                    R.CastOnUnit(target, true);
            }
        }

        static void KillSteal()
        {
            if (Option_Item("KillSteal Q"))
            {
                var Qtarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                var Qpredicition = Q.GetPrediction(Qtarget);
                if (Qtarget != null && Qpredicition.Hitchance >= HitChance.High && Qtarget.Health <= Q.GetDamage(Qtarget))
                    Q.Cast(Qtarget, true);
            }

            if (Option_Item("KillSteal E"))
            {
                var Etarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (Etarget != null && Etarget.Health <= E.GetDamage(Etarget))
                    E.Cast(Etarget, true);
            }

            if (Option_Item("KillSteal R"))
            {
                var Rtarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                if (Rtarget != null && Rtarget.Health <= R.GetDamage(Rtarget))
                    R.CastOnUnit(Rtarget, true);
            }
        }

        static void Flee()
        {
            if (Option.Item("FleeK").GetValue<KeyBind>().Active)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

                if (Q.IsReady())
                {
                    var step = Q.Range / 2;
                    for (var i = step; i <= Q.Range; i += step)
                    {
                        if (ObjectManager.Player.Position.Extend(Game.CursorPos, i).IsWall() && Player.Distance(Game.CursorPos) >= Q.Range / 2)
                            Q.Cast(Game.CursorPos);
                    }
                }
            }
        }

        static bool Option_Item(string IteamName)
        {
            return Option.Item(IteamName).GetValue<bool>();
        }

        static float GetComboDamage(Obj_AI_Hero Enemy)
        {
            float Damage = 0;

            float pass = 2 + (Player.Level * 6);

            Damage += pass; 

            if (Q.IsReady())
                Damage += Q.GetDamage(Enemy);

            if (E.IsReady())
                Damage += Q.GetDamage(Enemy);

            if (R.IsReady())
                Damage += R.GetDamage(Enemy);

            if (!Player.IsWindingUp)
                Damage += (float)ObjectManager.Player.GetAutoAttackDamage(Enemy, true);

            return Damage;
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Option_Item("Draw Q"))
                Render.Circle.DrawCircle(Player.Position, Q.Range, Color.White, 1);

            if (Option_Item("Draw Q Target"))
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (target != null && Q.IsReady())
                    Drawing.DrawCircle(target.Position, 150, Color.Green);
            }

            if (Option_Item("Draw E"))
                Render.Circle.DrawCircle(Player.Position, E.Range, Color.Yellow, 1);

            if (Option_Item("Draw R"))
                Render.Circle.DrawCircle(Player.Position, R.Range, Color.Green, 1);


            if (Option_Item("Draw R Target"))
            {
                var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                if (target != null && R.IsReady())
                    Drawing.DrawCircle(target.Position, 150, Color.Blue);
            }
        }

        public static void Drawing_OnEndScene(EventArgs args)
        {
            if (Player.IsDead) return;
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(ene => ene.IsValidTarget() && !ene.IsZombie))
            {
                if (Option_Item("DamageAfterCombo"))
                {
                    Indicator.unit = enemy;
                    Indicator.drawDmg(GetComboDamage(enemy), new ColorBGRA(255, 204, 0, 160));
                }
            }
        }

        static void MainMenu()
        {
            Option = new Menu("Sense Nautilus", "Sense_Nautilus", true).SetFontStyle(System.Drawing.FontStyle.Regular, SharpDX.Color.SkyBlue); ;

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Option.AddSubMenu(targetSelectorMenu);

            Option.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            orbWalker = new Orbwalking.Orbwalker(Option.SubMenu("Orbwalker"));

            var Harass = new Menu("Harass", "Harass");
            {
                Harass.AddItem(new MenuItem("Harass Q", "Use Q").SetValue(false));
                Harass.AddItem(new MenuItem("Harass W", "Use W").SetValue(true));
                Harass.AddItem(new MenuItem("Harass E", "Use E").SetValue(true));
            }
            Option.AddSubMenu(Harass);

            var LaneClear = new Menu("LaneClear", "LaneClear");
            {
                LaneClear.AddItem(new MenuItem("Lane Q", "Use Q").SetValue(true));
                LaneClear.AddItem(new MenuItem("Lane W", "Use W").SetValue(true));
                LaneClear.AddItem(new MenuItem("Lane E", "Use E").SetValue(true));
                LaneClear.AddItem(new MenuItem("LToggle", "Lane Clear Toggle").SetValue(new KeyBind('L', KeyBindType.Toggle)));
            }
                Option.AddSubMenu(LaneClear);

            var JungleClear = new Menu("JungleClear", "JungleClear");
            {
                JungleClear.AddItem(new MenuItem("Jungle Q", "Use Q").SetValue(true));
                JungleClear.AddItem(new MenuItem("Jungle W", "Use W").SetValue(true));
                JungleClear.AddItem(new MenuItem("Jungle E", "Use E").SetValue(true));
                JungleClear.AddItem(new MenuItem("JToggle", "Jungle Clear Toggle").SetValue(new KeyBind('J', KeyBindType.Toggle)));
            }
            Option.AddSubMenu(JungleClear);

            var Combo = new Menu("Combo", "Combo");
            {
                Combo.AddItem(new MenuItem("Combo Q", "Use Q").SetValue(true));
                Combo.AddItem(new MenuItem("Combo W", "Use W").SetValue(true));
                Combo.AddItem(new MenuItem("Combo E", "Use E").SetValue(true));
                Combo.AddItem(new MenuItem("Combo R", "Use R").SetValue(true));
            }
            Option.AddSubMenu(Combo);

            var Misc = new Menu("Misc", "Misc");
            {
                Misc.AddItem(new MenuItem("Fleek", "Flee Use Q").SetValue(new KeyBind('G', KeyBindType.Press)));
                Misc.SubMenu("Interrupt").AddItem(new MenuItem("Interrupt Q", "Use Q").SetValue(true));
                Misc.SubMenu("Interrupt").AddItem(new MenuItem("Interrupt R", "Use R").SetValue(true));
                Misc.SubMenu("KillSteal").AddItem(new MenuItem("KillSteal Q", "Use Q").SetValue(false));
                Misc.SubMenu("KillSteal").AddItem(new MenuItem("KillSteal E", "Use E").SetValue(true));
                Misc.SubMenu("KillSteal").AddItem(new MenuItem("KillSteal R", "Use R").SetValue(false));
            }
            Option.AddSubMenu(Misc);

            var Drawing = new Menu("Drawing", "Drawing");
            {
                Drawing.AddItem(new MenuItem("Draw Q", "Use Q").SetValue(false));
                Drawing.AddItem(new MenuItem("Draw Q Target", "Use Q (Target)").SetValue(true));
                Drawing.AddItem(new MenuItem("Draw E", "Use E").SetValue(false));
                Drawing.AddItem(new MenuItem("Draw R", "Use R").SetValue(false));
                Drawing.AddItem(new MenuItem("Draw R Target", "Use R (Target)").SetValue(true));
                Drawing.AddItem(new MenuItem("DamageAfterCombo", "Draw Combo Damage").SetValue(true));
            }
            Option.AddSubMenu(Drawing);

            Option.AddToMainMenu();
        }
    }
}
