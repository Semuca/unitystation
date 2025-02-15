﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public partial class BodyPart
	{
		/// <summary>
		/// The armor of the clothing covering a part of the body, ignoring selfArmor.
		/// </summary>
		private readonly LinkedList<Armor> clothingArmors = new LinkedList<Armor>();

		public LinkedList<Armor> ClothingArmors => clothingArmors;

		/// <summary>
		/// The armor of the body part itself, ignoring the clothing (for example the xenomorph's exoskeleton).
		/// </summary>
		[Tooltip("The armor of the body part itself, ignoring the clothing.")]
		public Armor SelfArmor = new Armor();

		/// The amount damage taken by body parts contained within this body part is modified by
		/// increases as this body part sustains more damage, to a maximum of 100% damage when the
		/// health of this part is 0.
		[Tooltip("The amount that damage taken by contained body parts is modified by")]
		public Armor SubOrganBodyPartArmour = new Armor();

		/// <summary>
		/// Threshold at which body parts contained within this body part start taking increased damage
		/// </summary>
		[Tooltip("Health threshold at which contained body parts start taking more damage")] [Range(0, 100)]
		public int SubOrganDamageIncreasePoint = 50;

		/// <summary>
		/// Determines whether dealing damage to this body part should impact the overall health of the creature
		/// </summary>
		[Tooltip("Does damaging this body part affect the creature's overall health?")]
		public bool DamageContributesToOverallHealth = true;

		/// <summary>
		/// Affects how much damage contributes to the efficiency of the body part, currently unimplemented
		/// </summary>
		public float DamageEfficiencyMultiplier = 1;

		/// <summary>
		/// Modifier that multiplicatively reduces the efficiency of the body part based on damage
		/// </summary>
		[Tooltip("Modifier to reduce efficiency with as damage is taken")]
		public Modifier DamageModifier = new Modifier();

		/// <summary>
		/// The body part's maximum health
		/// </summary>
		[Tooltip("This part's maximum health, which it will start at.")] [SerializeField]
		private float maxHealth = 100;

		public float MaxHealth => maxHealth;

		/// <summary>
		/// The body part's current health
		/// </summary>
		private float health;

		public float Health => health;

		/// <summary>
		/// Stores how severely the body part is damage for purposes of examine
		/// </summary>
		public DamageSeverity Severity = DamageSeverity.LightModerate;

		/// <summary>
		/// How much damage can this body part last before it breaks/gibs
		/// <summary>
		public float DamageThreshold = 18f;
		public DamageSeverity GibsOnSeverityLevel = DamageSeverity.Max;
		public float GibChance = 0.15f;

		/// <summary>
		/// Toxin damage taken
		/// </summary>
		public float Toxin => Damages[(int) DamageType.Tox];

		/// <summary>
		/// Brute damage taken
		/// </summary>
		public float Brute => Damages[(int) DamageType.Brute];

		/// <summary>
		/// Burn damage taken
		/// </summary>
		public float Burn => Damages[(int) DamageType.Burn];

		/// <summary>
		/// Cellular (clone) damage taken
		/// </summary>
		public float Cellular => Damages[(int) DamageType.Clone];

		/// <summary>
		/// Damage taken from lack of blood reagent
		/// </summary>
		public float Oxy => Damages[(int) DamageType.Oxy];

		/// <summary>
		/// Stamina damage taken
		/// </summary>
		public float Stamina => Damages[(int) DamageType.Stamina];

		/// <summary>
		/// Amount of radiation sustained. Not actually 'stacks' but rather a float.
		/// </summary>
		public float RadiationStacks => Damages[(int) DamageType.Radiation];

		/// <summary>
		/// List of all damage taken
		/// </summary>
		public readonly float[] Damages =
		{
			0,
			0,
			0,
			0,
			0,
			0,
			0
		};

		/// <summary>
		/// The total damage this body part has taken that is not from lack of blood reagent
		/// and not including cellular (clone) damage
		/// </summary>
		public float TotalDamageWithoutOxy
		{
			get
			{
				float TDamage = 0;
				for (int i = 0; i < Damages.Length; i++)
				{
					if ((int) DamageType.Oxy == i) continue;
					if ((int) DamageType.Radiation == i) continue;
					TDamage += Damages[i];
				}

				return TDamage;
			}
		}

		/// <summary>
		/// The total damage this body part has taken that is not from lack of blood reagent
		/// including cellular (clone) damage
		/// </summary>
		public float TotalDamageWithoutOxyCloneRadStam
		{
			get
			{
				float TDamage = 0;
				for (int i = 0; i < Damages.Length; i++)
				{
					if ((int) DamageType.Oxy == i) continue;
					if ((int) DamageType.Clone == i) continue;
					if ((int) DamageType.Radiation == i) continue;
					if ((int) DamageType.Stamina == i) continue;
					TDamage += Damages[i];
				}

				return TDamage;
			}
		}

		/// <summary>
		/// The total damage this body part has taken
		/// </summary>
		public float TotalDamage
		{
			get
			{
				float TDamage = 0;
				for (int i = 0; i < Damages.Length; i++)
				{
					if ((int) DamageType.Radiation == i) continue;
					TDamage += Damages[i];
				}

				return TDamage;
			}
		}

		public void DamageInitialisation()
		{
			this.AddModifier(DamageModifier);
		}

		/// <summary>
		/// Adjusts the appropriate damage type by the given damage amount and updates body part
		/// functionality based on its new health total
		/// </summary>
		/// <param name="damage">Damage amount</param>
		/// <param name="damageType">The type of damage</param>
		public void AffectDamage(float damage, int damageType)
		{
			float toDamage = Damages[damageType] + damage;

			if (toDamage < 0) toDamage = 0;

			Damages[damageType] = toDamage;
			health = maxHealth - TotalDamage;
			RecalculateEffectiveness();
			UpdateSeverity();
		}

		/// <summary>
		/// Applies damage to this body part. Damage will be divided among it and sub organs depending on their
		/// armor values.
		/// </summary>
		/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
		/// <param name="damage">Damage amount</param>
		/// <param name="attackType">Type of attack that is causing the damage</param>
		/// <param name="damageType">The type of damage</param>
		/// <param name="damageSplit">Should the damage be divided amongst the contained body parts or applied to a random body part</param>
		public void TakeDamage(
			GameObject damagedBy,
			float damage,
			AttackType attackType,
			DamageType damageType,
			bool damageSplit = false,
			bool DamageSubOrgans = true,
			float armorPenetration = 0
		)
		{
			float damageToLimb = Armor.GetTotalDamage(
				SelfArmor.GetDamage(damage, attackType, armorPenetration),
				attackType,
				ClothingArmors,
				armorPenetration
			);
			AffectDamage(damageToLimb, (int) damageType);

			// May be changed to individual damage
			// May also want it so it can miss sub organs
			if (DamageSubOrgans)
			{
				if (ContainBodyParts.Count > 0)
				{
					var organDamageRatingValue = SubOrganBodyPartArmour.GetRatingValue(attackType, armorPenetration);
					if (maxHealth - Damages[(int) damageType] < SubOrganDamageIncreasePoint)
					{
						organDamageRatingValue +=
							1 - ((maxHealth - Damages[(int) damageType]) / SubOrganDamageIncreasePoint);
						organDamageRatingValue = Math.Min(1, organDamageRatingValue);
					}

					var subDamage = damage * organDamageRatingValue;
					if (damageSplit)
					{
						foreach (var bodyPart in ContainBodyParts)
						{
							bodyPart.TakeDamage(damagedBy, subDamage / ContainBodyParts.Count, attackType, damageType,
								damageSplit);
						}
					}
					else
					{
						var OrganToDamage =
							ContainBodyParts.PickRandom(); //It's not like you can aim for Someone's liver can you
						OrganToDamage.TakeDamage(damagedBy, subDamage, attackType, damageType);
					}
				}
			}

			if(attackType == AttackType.Melee || attackType == AttackType.Laser || attackType == AttackType.Energy)
			{
				if(damageToLimb >= DamageThreshold)
				{
					CheckBodyPartIntigrity();
				}
			}
			if(attackType == AttackType.Bomb)
			{
				if(damageToLimb >= DamageThreshold)
				{
					DismemberBodyPartWithChance();
				}
			}
		}


		/// <summary>
		/// Heals damage taken by this body part
		/// </summary>
		public void HealDamage(GameObject healingItem, float healAmt,
			DamageType damageTypeToHeal)
		{
			AffectDamage(-healAmt, (int) damageTypeToHeal);
		}

		/// <summary>
		/// Heals damage taken by this body part
		/// </summary>
		public void HealDamage(GameObject healingItem, float healAmt,
			int damageTypeToHeal)
		{
			AffectDamage(-healAmt, damageTypeToHeal);
		}

		/// <summary>
		/// Resets all damage sustained by this body part
		/// </summary>
		public void ResetDamage()
		{
			for (int i = 0; i < Damages.Length; i++)
			{
				Damages[i] = 0;
			}

			health = maxHealth - TotalDamage;
			RecalculateEffectiveness();
			UpdateSeverity();
		}

		/// <summary>
		/// Recalculates the effectiveness of the organ based off of damage
		/// </summary>
		//Probably custom curves would be good here
		public void RecalculateEffectiveness()
		{
			DamageModifier.Multiplier = Mathf.Max(health, 0) / maxHealth;
		}

		/// <summary>
		/// Consumes radiation stacks and processes it into toxin damage
		/// </summary>
		public void CalculateRadiationDamage()
		{
			if (RadiationStacks == 0) return;
			var ProcessingRadiation = RadiationStacks * 0.001f;
			if (ProcessingRadiation < 20 && ProcessingRadiation > 0.5f)
			{
				ProcessingRadiation = 20;
			}

			AffectDamage(-ProcessingRadiation, (int) DamageType.Radiation);
			AffectDamage(ProcessingRadiation * 0.05f, (int) DamageType.Tox);
		}

		/// <summary>
		/// Updates the reported severity of injuries for examine based off of current health
		/// </summary>
		private void UpdateSeverity()
		{
			var oldSeverity = Severity;
			// update UI limbs depending on their severity of damage
			float severity = 1 - (Mathf.Max(maxHealth - TotalDamageWithoutOxyCloneRadStam, 0) / maxHealth);
			// If the limb is uninjured
			if (severity <= 0)
			{
				Severity = DamageSeverity.None;
			}
			// If the limb is under 10% damage
			else if (severity < 0.1)
			{
				Severity = DamageSeverity.Light;
			}
			// If the limb is under 25% damage
			else if (severity < 0.25)
			{
				Severity = DamageSeverity.LightModerate;
			}
			// If the limb is under 45% damage
			else if (severity < 0.45)
			{
				Severity = DamageSeverity.Moderate;
			}
			// If the limb is under 85% damage
			else if (severity < 0.85)
			{
				Severity = DamageSeverity.Bad;
			}
			// If the limb is under 95% damage
			else if (severity < 0.95f)
			{
				Severity = DamageSeverity.Critical;
			}
			// If the limb is 95% damage or over
			else if (severity >= 0.95f)
			{
				Severity = DamageSeverity.Max;
			}

			if (oldSeverity != Severity && healthMaster != null)
			{
				UpdateIcons();
			}
		}

		/// <summary>
		/// Checks if the bodypart is damaged to a point where it can be gibbed from the body
		/// </summary>
		protected void CheckBodyPartIntigrity()
		{
			if(Severity >= GibsOnSeverityLevel)
			{
				DismemberBodyPartWithChance();
			}
		}

		/// <summary>
		/// Checks if the player is lucky enough and is wearing enough protective armor to avoid getting his bodypart removed.
		/// </summary>
		public void DismemberBodyPartWithChance()
		{
			float chance = UnityEngine.Random.RandomRange(0.0f, 1.0f);
			float armorChanceModifer = GibChance + SelfArmor.DismembermentProtectionChance;
			if(Severity == DamageSeverity.Max){armorChanceModifer -= 0.25f;} //Make it more likely that the bodypart can be gibbed in it's worst condition.
			if(chance >= armorChanceModifer)
			{
				RemoveFromBodyThis();
			}
		}

		/// <summary>
		/// Updates the player health UI if present
		/// </summary>
		private void UpdateIcons()
		{
			UIManager.PlayerHealthUI.SetBodyTypeOverlay(this);
		}
	}
}
