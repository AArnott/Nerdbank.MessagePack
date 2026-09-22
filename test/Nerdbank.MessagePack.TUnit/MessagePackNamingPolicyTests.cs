// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// Verifies property name transformation behaviors of <see cref="MessagePackNamingPolicy"/>.
/// </summary>
/// <remarks>
/// A lot of these test cases come from <see href="https://github.com/dotnet/runtime/blob/b2974279efd059efaa17f359ed4b266b1c705721/src/libraries/System.Text.Json/tests/System.Text.Json.Tests/Serialization/NamingPolicyUnitTests.cs#L12">System.Text.Json tests</see>.
/// </remarks>
public class MessagePackNamingPolicyTests
{
	[Test]
	[Arguments("urlValue", "URLValue")]
	[Arguments("url", "URL")]
	[Arguments("id", "ID")]
	[Arguments("i", "I")]
	[Arguments("", "")]
	[Arguments("😀葛🀄", "😀葛🀄")] // Surrogate pairs
	[Arguments("άλφαΒήταΓάμμα", "ΆλφαΒήταΓάμμα")] // Non-ascii letters
	[Arguments("𐐀𐐨𐐨𐐀𐐨𐐨", "𐐀𐐨𐐨𐐀𐐨𐐨")] // Surrogate pair letters don't normalize
	[Arguments("\ude00\ud83d", "\ude00\ud83d")] // Unpaired surrogates
	[Arguments("person", "Person")]
	[Arguments("iPhone", "iPhone")]
	[Arguments("iPhone", "IPhone")]
	[Arguments("iPhone", "I Phone")]
	[Arguments("iPhone", "I  Phone")]
	[Arguments("iPhone", " IPhone")]
	[Arguments("iPhone", " IPhone ")]
	[Arguments("isCia", "IsCIA")]
	[Arguments("vmQ", "VmQ")]
	[Arguments("xml2Json", "Xml2Json")]
	[Arguments("snAkEcAsE", "SnAkEcAsE")]
	[Arguments("snA__kEcAsE", "SnA__kEcAsE")]
	[Arguments("snA__kEcAsE", "SnA__ kEcAsE")]
	[Arguments("already_snake_case_", "already_snake_case_ ")]
	[Arguments("isJsonProperty", "IsJSONProperty")]
	[Arguments("shouting_case", "SHOUTING_CASE")]
	[Arguments("9999-12-31T23:59:59.9999999Z", "9999-12-31T23:59:59.9999999Z")]
	[Arguments("hi!!thisIsText.timeToTest.", "Hi!! This is text. Time to test.")]
	[Arguments("building", "BUILDING")]
	[Arguments("buildingProperty", "BUILDING Property")]
	[Arguments("buildingProperty", "Building Property")]
	[Arguments("buildingProperty", "BUILDING PROPERTY")]
	public void CamelCaseNamingPolicy(string expected, string input)
	{
		string actual = MessagePackNamingPolicy.CamelCase.ConvertName(input);
		Assert.Equal(expected, actual);
	}

	[Test]
	[Arguments("PropertyName", "propertyName")]
	[Arguments("PropertyName", "propertyNAME")]
	[Arguments("UrlValue", "urlValue")]
	[Arguments("Url", "url")]
	[Arguments("Id", "id")]
	[Arguments("I", "i")]
	[Arguments("Person", "person")]
	[Arguments("IPhone", "iPhone")]
	[Arguments("IPhone", "i Phone")]
	[Arguments("MyUrl", "myURL")]
	[Arguments("MyUrlValue", "myURLValue")]
	[Arguments("AlreadyPascal", "AlreadyPascal")]
	[Arguments("This_is_a_test", "THIS_IS_A_TEST")]
	[Arguments("ThisIsATest", "thisIsATest")]
	[Arguments("Sha512Hash", "SHA512Hash")]
	[Arguments("MyProperty", "My Property")]
	[Arguments("", " ")]
	[Arguments("", "")]
	public void PascalCaseNamingPolicy(string expected, string input)
	{
		string actual = MessagePackNamingPolicy.PascalCase.ConvertName(input);
		Assert.Equal(expected, actual);
	}

	[Test]
	public void CamelCaseNullNameThrows()
	{
		Assert.Throws<ArgumentNullException>(() => MessagePackNamingPolicy.CamelCase.ConvertName(null!));
	}

	[Test]
	[Arguments("xml_http_request", "XMLHttpRequest")]
	[Arguments("sha512_hash_algorithm", "SHA512HashAlgorithm")]
	[Arguments("i18n", "i18n")]
	[Arguments("i18n_policy", "I18nPolicy")]
	[Arguments("7samurai", "7samurai")]
	[Arguments("camel_case", "camelCase")]
	[Arguments("camel_case", "CamelCase")]
	[Arguments("snake_case", "snake_case")]
	[Arguments("snake_case", "SNAKE_CASE")]
	[Arguments("kebab-case", "kebab-case")]
	[Arguments("kebab-case", "KEBAB-CASE")]
	[Arguments("double_space", "double  space")]
	[Arguments("double__underscore", "double__underscore")]
	[Arguments("double--dash", "double--dash")]
	[Arguments("abc", "abc")]
	[Arguments("ab_c", "abC")]
	[Arguments("a_bc", "aBc")]
	[Arguments("a_bc", "aBC")]
	[Arguments("a_bc", "ABc")]
	[Arguments("abc", "ABC")]
	[Arguments("abc123def456", "abc123def456")]
	[Arguments("abc123_def456", "abc123Def456")]
	[Arguments("abc123_def456", "abc123DEF456")]
	[Arguments("abc123_def456", "ABC123DEF456")]
	[Arguments("abc123def456", "ABC123def456")]
	[Arguments("abc123def456", "Abc123def456")]
	[Arguments("abc", "  abc")]
	[Arguments("abc", "abc  ")]
	[Arguments("abc", "  abc  ")]
	[Arguments("abc", "  Abc  ")]
	[Arguments("7ab7", "  7ab7  ")]
	[Arguments("abc_def", "  abc def  ")]
	[Arguments("abc_def", "  abc  def  ")]
	[Arguments("abc_def", "  abc   def  ")]
	[Arguments("abc_7ef", "  abc 7ef  ")]
	[Arguments("ab7_def", "  ab7 def  ")]
	[Arguments("_abc", "_abc")]
	[Arguments("", "")]
	[Arguments("😀葛🀄", "😀葛🀄")] // Surrogate pairs
	[Arguments("άλφα_βήτα_γάμμα", "ΆλφαΒήταΓάμμα")] // Non-ascii letters
	[Arguments("𐐀𐐨𐐨𐐀𐐨𐐨", "𐐀𐐨𐐨𐐀𐐨𐐨")] // Surrogate pair letters don't normalize
	[Arguments("𐐀abc_def𐐨abc😀def𐐀", "𐐀AbcDef𐐨Abc😀Def𐐀")]
	[Arguments("\ude00\ud83d", "\ude00\ud83d")] // Unpaired surrogates
	[Arguments("a%", "a%")]
	[Arguments("_?#-", "_?#-")]
	[Arguments("?!?", "? ! ?")]
	[Arguments("$type", "$type")]
	[Arguments("abc%def", "abc%def")]
	[Arguments("__abc__def__", "__abc__def__")]
	[Arguments("_abc_abc_abc", "_abcAbc_abc")]
	[Arguments("abc???def", "ABC???def")]
	[Arguments("ab_cd-_-de_f", "ABCd  - _ -   DE f")]
	[Arguments(
			"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
			"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
	[Arguments(
			"a_haaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
			"aHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
	[Arguments(
			"a_towel_it_says_is_about_the_most_massively_useful_thing_an_interstellar_hitchhiker_can_have_partly_it_has_great_practical_value_you_can_wrap_it_around_you_for_warmth_as_you_bound_across_the_cold_moons_of_jaglan_beta_you_can_lie_on_it_on_the_brilliant_marble_sanded_beaches_of_santraginus_v_inhaling_the_heady_sea_vapors_you_can_sleep_under_it_beneath_the_stars_which_shine_so_redly_on_the_desert_world_of_kakrafoon_use_it_to_sail_a_miniraft_down_the_slow_heavy_river_moth_wet_it_for_use_in_hand_to_hand_combat_wrap_it_round_your_head_to_ward_off_noxious_fumes_or_avoid_the_gaze_of_the_ravenous_bugblatter_beast_of_traal_a_mind_bogglingly_stupid_animal_it_assumes_that_if_you_cant_see_it_it_cant_see_you_daft_as_a_brush_but_very_very_ravenous_you_can_wave_your_towel_in_emergencies_as_a_distress_signal_and_of_course_dry_yourself_of_with_it_if_it_still_seems_to_be_clean_enough",
			"ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough")]
	public void SnakeLowerCaseNamingPolicy(string expected, string input)
	{
		string actual = MessagePackNamingPolicy.SnakeLowerCase.ConvertName(input);
		Assert.Equal(expected, actual);
	}

	[Test]
	[Arguments("XML_HTTP_REQUEST", "XMLHttpRequest")]
	[Arguments("SHA512_HASH_ALGORITHM", "SHA512HashAlgorithm")]
	[Arguments("I18N", "i18n")]
	[Arguments("I18N_POLICY", "I18nPolicy")]
	[Arguments("7SAMURAI", "7samurai")]
	[Arguments("CAMEL_CASE", "camelCase")]
	[Arguments("CAMEL_CASE", "CamelCase")]
	[Arguments("SNAKE_CASE", "snake_case")]
	[Arguments("SNAKE_CASE", "SNAKE_CASE")]
	[Arguments("KEBAB-CASE", "kebab-case")]
	[Arguments("KEBAB-CASE", "KEBAB-CASE")]
	[Arguments("DOUBLE_SPACE", "double  space")]
	[Arguments("DOUBLE__UNDERSCORE", "double__underscore")]
	[Arguments("DOUBLE--DASH", "double--dash")]
	[Arguments("ABC", "abc")]
	[Arguments("AB_C", "abC")]
	[Arguments("A_BC", "aBc")]
	[Arguments("A_BC", "aBC")]
	[Arguments("A_BC", "ABc")]
	[Arguments("ABC", "ABC")]
	[Arguments("ABC123DEF456", "abc123def456")]
	[Arguments("ABC123_DEF456", "abc123Def456")]
	[Arguments("ABC123_DEF456", "abc123DEF456")]
	[Arguments("ABC123_DEF456", "ABC123DEF456")]
	[Arguments("ABC123DEF456", "ABC123def456")]
	[Arguments("ABC123DEF456", "Abc123def456")]
	[Arguments("ABC", "  ABC")]
	[Arguments("ABC", "ABC  ")]
	[Arguments("ABC", "  ABC  ")]
	[Arguments("ABC", "  Abc  ")]
	[Arguments("7AB7", "  7ab7  ")]
	[Arguments("ABC_DEF", "  ABC def  ")]
	[Arguments("ABC_DEF", "  abc  def  ")]
	[Arguments("ABC_DEF", "  abc   def  ")]
	[Arguments("ABC_7EF", "  abc 7ef  ")]
	[Arguments("AB7_DEF", "  ab7 def  ")]
	[Arguments("_ABC", "_abc")]
	[Arguments("", "")]
	[Arguments("😀葛🀄", "😀葛🀄")] // Surrogate pairs
	[Arguments("ΆΛΦΑ_ΒΉΤΑ_ΓΆΜΜΑ", "ΆλφαΒήταΓάμμα")] // Non-ascii letters
	[Arguments("𐐀𐐨𐐨𐐀𐐨𐐨", "𐐀𐐨𐐨𐐀𐐨𐐨")] // Surrogate pair letters don't normalize
	[Arguments("𐐀ABC_DEF𐐨ABC😀DEF𐐀", "𐐀AbcDef𐐨Abc😀Def𐐀")]
	[Arguments("\ude00\ud83d", "\ude00\ud83d")] // Unpaired surrogates
	[Arguments("A%", "a%")]
	[Arguments("_?#-", "_?#-")]
	[Arguments("?!?", "? ! ?")]
	[Arguments("$TYPE", "$type")]
	[Arguments("ABC%DEF", "abc%def")]
	[Arguments("__ABC__DEF__", "__abc__def__")]
	[Arguments("_ABC_ABC_ABC", "_abcAbc_abc")]
	[Arguments("ABC???DEF", "ABC???def")]
	[Arguments("AB_CD-_-DE_F", "ABCd  - _ -   DE f")]
	[Arguments(
			"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
			"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
	[Arguments(
			"A_HAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
			"aHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
	[Arguments(
			"A_TOWEL_IT_SAYS_IS_ABOUT_THE_MOST_MASSIVELY_USEFUL_THING_AN_INTERSTELLAR_HITCHHIKER_CAN_HAVE_PARTLY_IT_HAS_GREAT_PRACTICAL_VALUE_YOU_CAN_WRAP_IT_AROUND_YOU_FOR_WARMTH_AS_YOU_BOUND_ACROSS_THE_COLD_MOONS_OF_JAGLAN_BETA_YOU_CAN_LIE_ON_IT_ON_THE_BRILLIANT_MARBLE_SANDED_BEACHES_OF_SANTRAGINUS_V_INHALING_THE_HEADY_SEA_VAPORS_YOU_CAN_SLEEP_UNDER_IT_BENEATH_THE_STARS_WHICH_SHINE_SO_REDLY_ON_THE_DESERT_WORLD_OF_KAKRAFOON_USE_IT_TO_SAIL_A_MINIRAFT_DOWN_THE_SLOW_HEAVY_RIVER_MOTH_WET_IT_FOR_USE_IN_HAND_TO_HAND_COMBAT_WRAP_IT_ROUND_YOUR_HEAD_TO_WARD_OFF_NOXIOUS_FUMES_OR_AVOID_THE_GAZE_OF_THE_RAVENOUS_BUGBLATTER_BEAST_OF_TRAAL_A_MIND_BOGGLINGLY_STUPID_ANIMAL_IT_ASSUMES_THAT_IF_YOU_CANT_SEE_IT_IT_CANT_SEE_YOU_DAFT_AS_A_BRUSH_BUT_VERY_VERY_RAVENOUS_YOU_CAN_WAVE_YOUR_TOWEL_IN_EMERGENCIES_AS_A_DISTRESS_SIGNAL_AND_OF_COURSE_DRY_YOURSELF_OF_WITH_IT_IF_IT_STILL_SEEMS_TO_BE_CLEAN_ENOUGH",
			"ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough")]
	public void SnakeUpperCaseNamingPolicy(string expected, string input)
	{
		string actual = MessagePackNamingPolicy.SnakeUpperCase.ConvertName(input);
		Assert.Equal(expected, actual);
	}

	[Test]
	[Arguments("xml-http-request", "XMLHttpRequest")]
	[Arguments("sha512-hash-algorithm", "SHA512HashAlgorithm")]
	[Arguments("i18n", "i18n")]
	[Arguments("i18n-policy", "I18nPolicy")]
	[Arguments("7samurai", "7samurai")]
	[Arguments("camel-case", "camelCase")]
	[Arguments("camel-case", "CamelCase")]
	[Arguments("snake_case", "snake_case")]
	[Arguments("snake_case", "SNAKE_CASE")]
	[Arguments("kebab-case", "kebab-case")]
	[Arguments("kebab-case", "KEBAB-CASE")]
	[Arguments("double-space", "double  space")]
	[Arguments("double__underscore", "double__underscore")]
	[Arguments("double--dash", "double--dash")]
	[Arguments("abc", "abc")]
	[Arguments("ab-c", "abC")]
	[Arguments("a-bc", "aBc")]
	[Arguments("a-bc", "aBC")]
	[Arguments("a-bc", "ABc")]
	[Arguments("abc", "ABC")]
	[Arguments("abc123def456", "abc123def456")]
	[Arguments("abc123-def456", "abc123Def456")]
	[Arguments("abc123-def456", "abc123DEF456")]
	[Arguments("abc123-def456", "ABC123DEF456")]
	[Arguments("abc123def456", "ABC123def456")]
	[Arguments("abc123def456", "Abc123def456")]
	[Arguments("abc", "  abc")]
	[Arguments("abc", "abc  ")]
	[Arguments("abc", "  abc  ")]
	[Arguments("abc", "  Abc  ")]
	[Arguments("7ab7", "  7ab7  ")]
	[Arguments("abc-def", "  abc def  ")]
	[Arguments("abc-def", "  abc  def  ")]
	[Arguments("abc-def", "  abc   def  ")]
	[Arguments("abc-7ef", "  abc 7ef  ")]
	[Arguments("ab7-def", "  ab7 def  ")]
	[Arguments("-abc", "-abc")]
	[Arguments("", "")]
	[Arguments("😀葛🀄", "😀葛🀄")] // Surrogate pairs
	[Arguments("άλφα-βήτα-γάμμα", "ΆλφαΒήταΓάμμα")] // Non-ascii letters
	[Arguments("𐐀𐐨𐐨𐐀𐐨𐐨", "𐐀𐐨𐐨𐐀𐐨𐐨")] // Surrogate pair letters don't normalize
	[Arguments("𐐀abc-def𐐨abc😀def𐐀", "𐐀AbcDef𐐨Abc😀Def𐐀")]
	[Arguments("\ude00\ud83d", "\ude00\ud83d")] // Unpaired surrogates
	[Arguments("a%", "a%")]
	[Arguments("-?#_", "-?#_")]
	[Arguments("?!?", "? ! ?")]
	[Arguments("$type", "$type")]
	[Arguments("abc%def", "abc%def")]
	[Arguments("--abc--def--", "--abc--def--")]
	[Arguments("-abc-abc-abc", "-abcAbc-abc")]
	[Arguments("abc???def", "ABC???def")]
	[Arguments("ab-cd-_-de-f", "ABCd  - _ -   DE f")]
	[Arguments(
			  "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
			  "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
	[Arguments(
			  "a-haaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
			  "aHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
	[Arguments(
			  "a-towel-it-says-is-about-the-most-massively-useful-thing-an-interstellar-hitchhiker-can-have_partly-it-has-great-practical-value_you-can-wrap-it-around-you-for-warmth-as-you-bound-across-the-cold-moons-of-jaglan-beta_you-can-lie-on-it-on-the-brilliant-marble-sanded-beaches-of-santraginus-v-inhaling-the-heady-sea-vapors_you-can-sleep-under-it-beneath-the-stars-which-shine-so-redly-on-the-desert-world-of-kakrafoon_use-it-to-sail-a-miniraft-down-the-slow-heavy-river-moth_wet-it-for-use-in-hand-to-hand-combat_wrap-it-round-your-head-to-ward-off-noxious-fumes-or-avoid-the-gaze-of-the-ravenous-bugblatter-beast-of-traal-a-mind-bogglingly-stupid-animal_it-assumes-that-if-you-cant-see-it-it-cant-see-you-daft-as-a-brush-but-very-very-ravenous_you-can-wave-your-towel-in-emergencies-as-a-distress-signal-and-of-course-dry-yourself-of-with-it-if-it-still-seems-to-be-clean-enough",
			  "ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough")]
	public void KebabLowerCaseNamingPolicy(string expected, string input)
	{
		string actual = MessagePackNamingPolicy.KebabLowerCase.ConvertName(input);
		Assert.Equal(expected, actual);
	}

	[Test]
	[Arguments("XML-HTTP-REQUEST", "XMLHttpRequest")]
	[Arguments("SHA512-HASH-ALGORITHM", "SHA512HashAlgorithm")]
	[Arguments("I18N", "i18n")]
	[Arguments("I18N-POLICY", "I18nPolicy")]
	[Arguments("7SAMURAI", "7samurai")]
	[Arguments("CAMEL-CASE", "camelCase")]
	[Arguments("CAMEL-CASE", "CamelCase")]
	[Arguments("SNAKE_CASE", "snake_case")]
	[Arguments("SNAKE_CASE", "SNAKE_CASE")]
	[Arguments("KEBAB-CASE", "kebab-case")]
	[Arguments("KEBAB-CASE", "KEBAB-CASE")]
	[Arguments("DOUBLE-SPACE", "double  space")]
	[Arguments("DOUBLE__UNDERSCORE", "double__underscore")]
	[Arguments("DOUBLE--DASH", "double--dash")]
	[Arguments("ABC", "abc")]
	[Arguments("AB-C", "abC")]
	[Arguments("A-BC", "aBc")]
	[Arguments("A-BC", "aBC")]
	[Arguments("A-BC", "ABc")]
	[Arguments("ABC", "ABC")]
	[Arguments("ABC123DEF456", "abc123def456")]
	[Arguments("ABC123-DEF456", "abc123Def456")]
	[Arguments("ABC123-DEF456", "abc123DEF456")]
	[Arguments("ABC123-DEF456", "ABC123DEF456")]
	[Arguments("ABC123DEF456", "ABC123def456")]
	[Arguments("ABC123DEF456", "Abc123def456")]
	[Arguments("ABC", "  ABC")]
	[Arguments("ABC", "ABC  ")]
	[Arguments("ABC", "  ABC  ")]
	[Arguments("ABC", "  Abc  ")]
	[Arguments("7AB7", "  7ab7  ")]
	[Arguments("ABC-DEF", "  ABC def  ")]
	[Arguments("ABC-DEF", "  abc  def  ")]
	[Arguments("ABC-DEF", "  abc   def  ")]
	[Arguments("ABC-7EF", "  abc 7ef  ")]
	[Arguments("AB7-DEF", "  ab7 def  ")]
	[Arguments("-ABC", "-abc")]
	[Arguments("", "")]
	[Arguments("😀葛🀄", "😀葛🀄")] // Surrogate pairs
	[Arguments("ΆΛΦΑ-ΒΉΤΑ-ΓΆΜΜΑ", "ΆλφαΒήταΓάμμα")] // Non-ascii letters
	[Arguments("𐐀𐐨𐐨𐐀𐐨𐐨", "𐐀𐐨𐐨𐐀𐐨𐐨")] // Surrogate pair letters don't normalize
	[Arguments("𐐀ABC-DEF𐐨ABC😀DEF𐐀", "𐐀AbcDef𐐨Abc😀Def𐐀")]
	[Arguments("\ude00\ud83d", "\ude00\ud83d")] // Unpaired surrogates
	[Arguments("A%", "a%")]
	[Arguments("-?#_", "-?#_")]
	[Arguments("?!?", "? ! ?")]
	[Arguments("$TYPE", "$type")]
	[Arguments("ABC%DEF", "abc%def")]
	[Arguments("--ABC--DEF--", "--abc--def--")]
	[Arguments("-ABC-ABC-ABC", "-abcAbc-abc")]
	[Arguments("ABC???DEF", "ABC???def")]
	[Arguments("AB-CD-_-DE-F", "ABCd  - _ -   DE f")]
	[Arguments(
			  "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
			  "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
	[Arguments(
			  "A-HAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
			  "aHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
	[Arguments(
			  "A-TOWEL-IT-SAYS-IS-ABOUT-THE-MOST-MASSIVELY-USEFUL-THING-AN-INTERSTELLAR-HITCHHIKER-CAN-HAVE_PARTLY-IT-HAS-GREAT-PRACTICAL-VALUE_YOU-CAN-WRAP-IT-AROUND-YOU-FOR-WARMTH-AS-YOU-BOUND-ACROSS-THE-COLD-MOONS-OF-JAGLAN-BETA_YOU-CAN-LIE-ON-IT-ON-THE-BRILLIANT-MARBLE-SANDED-BEACHES-OF-SANTRAGINUS-V-INHALING-THE-HEADY-SEA-VAPORS_YOU-CAN-SLEEP-UNDER-IT-BENEATH-THE-STARS-WHICH-SHINE-SO-REDLY-ON-THE-DESERT-WORLD-OF-KAKRAFOON_USE-IT-TO-SAIL-A-MINIRAFT-DOWN-THE-SLOW-HEAVY-RIVER-MOTH_WET-IT-FOR-USE-IN-HAND-TO-HAND-COMBAT_WRAP-IT-ROUND-YOUR-HEAD-TO-WARD-OFF-NOXIOUS-FUMES-OR-AVOID-THE-GAZE-OF-THE-RAVENOUS-BUGBLATTER-BEAST-OF-TRAAL-A-MIND-BOGGLINGLY-STUPID-ANIMAL_IT-ASSUMES-THAT-IF-YOU-CANT-SEE-IT-IT-CANT-SEE-YOU-DAFT-AS-A-BRUSH-BUT-VERY-VERY-RAVENOUS_YOU-CAN-WAVE-YOUR-TOWEL-IN-EMERGENCIES-AS-A-DISTRESS-SIGNAL-AND-OF-COURSE-DRY-YOURSELF-OF-WITH-IT-IF-IT-STILL-SEEMS-TO-BE-CLEAN-ENOUGH",
			  "ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough")]
	public void KebabUpperCaseNamingPolicy(string expected, string input)
	{
		string actual = MessagePackNamingPolicy.KebabUpperCase.ConvertName(input);
		Assert.Equal(expected, actual);
	}
}
