﻿Public Class Form1

    Enum RecruitmentType
        RecruitmentTypeNormal
        RecruitmentTypeReverse
        RecruitmentTypeRandom
    End Enum

    Enum EnemyBuffType
        BuffTypeSetConstant
        BuffTypeSetMaximum
        BuffTypeSetMinimum
    End Enum

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles BrowseButton.Click
        OpenFileDialog1.ShowDialog()
    End Sub

    Private type As Utilities.GameType

    Private shouldRandomizeGrowths As Boolean
    Private growthsVariance As Integer
    Private shouldForceMinimumGrowth As Boolean
    Private shouldWeightHPGrowths As Boolean

    Private shouldRandomizeBases As Boolean
    Private baseVariance As Integer
    Private shouldRandomizeCON As Boolean
    Private minimumCON As Integer
    Private shouldRandomizeMOV As Boolean
    Private minimumMOV As Integer
    Private maximumMOV As Integer

    Private shouldRandomizeAffinity As Boolean

    Private shouldRandomizeAffinityBonuses As Boolean
    Private affinityBonusMaxMultiplier As Integer

    Private shouldRandomizeClasses As Boolean
    Private randomLords As Boolean
    Private randomThieves As Boolean
    Private randomBosses As Boolean
    Private uniqueClasses As Boolean

    Private shouldRandomizeItems As Boolean
    Private mightVariance As Integer
    Private minimumMight As Integer
    Private hitVariance As Integer
    Private minimumHit As Integer
    Private criticalVariance As Integer
    Private minimumCritical As Integer
    Private weightVariance As Integer
    Private minimumWeight As Integer
    Private maximumWeight As Integer
    Private durabilityVariance As Integer
    Private minimumDurability As Integer
    Private randomTraits As Boolean

    Private buffEnemies As Boolean
    Private buffAmount As Integer
    Private buffType As EnemyBuffType
    Private buffBosses As Boolean

    Private recruitment As RecruitmentType


    Private Sub OpenFileDialog1_FileOk(ByVal sender As System.Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles OpenFileDialog1.FileOk
        FilenameTextBox.Text = OpenFileDialog1.FileName

        Dim inputFile = IO.File.OpenRead(OpenFileDialog1.FileName)

        inputFile.Seek(&HAC, IO.SeekOrigin.Begin)

        Dim gameCode As ArrayList = Utilities.ReadWordIntoArrayListFromFile(inputFile)

        'Known Game Codes
        ' FE6 - AFEJ (JP)
        ' FE7 - AE7E (NA)
        ' FE8 - BE8E (NA)

        type = Utilities.GameType.GameTypeUnknown

        If gameCode.Item(0).Equals(Asc("A")) And gameCode.Item(1).Equals(Asc("F")) And gameCode.Item(2).Equals(Asc("E")) And gameCode.Item(3).Equals(Asc("J")) Then type = Utilities.GameType.GameTypeFE6
        If gameCode.Item(0).Equals(Asc("A")) And gameCode.Item(1).Equals(Asc("E")) And gameCode.Item(2).Equals(Asc("7")) And gameCode.Item(3).Equals(Asc("E")) Then type = Utilities.GameType.GameTypeFE7
        If gameCode.Item(0).Equals(Asc("B")) And gameCode.Item(1).Equals(Asc("E")) And gameCode.Item(2).Equals(Asc("8")) And gameCode.Item(3).Equals(Asc("E")) Then type = Utilities.GameType.GameTypeFE8

        If type = Utilities.GameType.GameTypeFE6 Then
            GameDetectionLabel.Text = "Game Detected: Fire Emblem 6"
            RandomizeButton.Enabled = True
        ElseIf type = Utilities.GameType.GameTypeFE7 Then
            GameDetectionLabel.Text = "Game Detected: Fire Emblem 7"
            RandomizeButton.Enabled = True
        ElseIf type = Utilities.GameType.GameTypeFE8 Then
            GameDetectionLabel.Text = "Game Detected: Fire Emblem 8"
            RandomizeButton.Enabled = True
        Else
            GameDetectionLabel.Text = "Unknown Game (Code: " + Convert.ToChar(gameCode.Item(0)) + Convert.ToChar(gameCode.Item(1)) + Convert.ToChar(gameCode.Item(2)) + Convert.ToChar(gameCode.Item(3)) + ")"
            RandomizeButton.Enabled = False
        End If

        inputFile.Close()

        If type <> Utilities.GameType.GameTypeUnknown Then
            reenableAllControls()

            Dim hasSeenNotice As Boolean = My.Settings.HasSeenBetaNotice
            If Not hasSeenNotice Then
                MsgBox("Thanks for testing out the Universal GBA FE Randomizer!" + vbCrLf + vbCrLf _
                           & "This is an early build of the app that is still missing several features, but I figure it should be good enough to test with so that we catch any other issues I'm not aware of sooner rather than later." + vbCrLf + vbCrLf _
                           & "Controls that are disabled are likely not yet implemented. Most notably, Random classes has only been implemented with FE6. FE7 and FE8 should soon follow." + vbCrLf + vbCrLf _
                           & "Please report any issues you encounter when using this app. You can share a randomized instance of a game using NUPS to create ups patches with your randomized info. Please do so to report issues." + vbCrLf + vbCrLf _
                           & "Thanks again!" + vbCrLf + vbCrLf _
                           & "- /u/OtakuReborn", MsgBoxStyle.Information, "Notice")

                My.Settings.HasSeenBetaNotice = True
                My.Settings.Save()
            End If
        End If

    End Sub

    Private Sub RandomizeButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RandomizeButton.Click

        disableAllControls()

        Dim characterList As ArrayList = New ArrayList()
        Dim characterLookup As Hashtable = New Hashtable()
        Dim characterTableOffset As Integer = 0
        Dim numberOfCharacters As Integer = 0
        Dim characterEntrySize As Integer = 0

        Dim classList As ArrayList = New ArrayList()
        Dim classLookup As Hashtable = New Hashtable()
        Dim classTableOffset As Integer = 0
        Dim numberOfClasses As Integer = 0
        Dim classEntrySize As Integer = 0

        Dim itemList As ArrayList = New ArrayList()
        Dim itemLookup As Hashtable = New Hashtable()
        Dim itemTableOffset As Integer = 0
        Dim numberOfItems As Integer = 0
        Dim itemEntrySize As Integer = 0

        Dim repointedCharacterTable = False
        Dim repointedClassTable = False
        Dim repointedItemTable = False

        Dim chapterUnitData As ArrayList = New ArrayList()

        Dim lordCharacters As ArrayList = New ArrayList()
        Dim thiefCharacters As ArrayList = New ArrayList()
        Dim bossCharacters As ArrayList = New ArrayList()
        Dim exemptCharacters As ArrayList = New ArrayList()

        Dim fileReader = IO.File.OpenRead(OpenFileDialog1.FileName)

        If type = Utilities.GameType.GameTypeFE6 Then
            fileReader.Seek(FE6GameData.PointerToCharacterTableOffset, IO.SeekOrigin.Begin)
            characterTableOffset = Utilities.ReadWord(fileReader, True)
            If characterTableOffset <> FE6GameData.DefaultCharacterTableOffset Then
                MsgBox("Character Table Offset has been updated. Character Table may have been repointed." + vbCrLf _
                       & "If this is a hacked game, not all characters may be randomized if the table is expanded." + vbCrLf _
                       & "Determining which units are enemies and allies may also be inaccurate.", MsgBoxStyle.Information, "Notice")
                repointedCharacterTable = True
            End If
            numberOfCharacters = FE6GameData.CharacterCount
            characterEntrySize = FE6GameData.CharacterEntrySize

            fileReader.Seek(FE6GameData.PointerToClassTableOffset, IO.SeekOrigin.Begin)
            classTableOffset = Utilities.ReadWord(fileReader, True)
            If classTableOffset <> FE6GameData.DefaultClassTableOffset Then
                MsgBox("Class Table Offset has been updated. Class Table may have been repointed." + vbCrLf _
                       & "If the number of classes has changed, only the classes up to the original amount" + vbCrLf _
                       & "will be updated and used.", MsgBoxStyle.Information, "Notice")
                repointedClassTable = True
            End If
            numberOfClasses = FE6GameData.ClassCount
            classEntrySize = FE6GameData.ClassEntrySize

            fileReader.Seek(FE6GameData.PointerToItemTableOffset, IO.SeekOrigin.Begin)
            itemTableOffset = Utilities.ReadWord(fileReader, True)
            If itemTableOffset <> FE6GameData.DefaultItemTableOffset Then
                MsgBox("Item Table Offset has been updated. Item Table may have been repointed." + vbCrLf _
                       & "If the number of items has changed, only the items up to the original amount" + vbCrLf _
                       & "will be updated and used.", MsgBoxStyle.Information, "Notice")
                repointedItemTable = True
            End If
            numberOfItems = FE6GameData.ItemCount
            itemEntrySize = FE6GameData.ItemEntrySize

            Dim chapterPointers As Array = System.Enum.GetValues(GetType(FE6GameData.ChapterUnitReference))
            Dim chapterUnitCounts As ArrayList = FE6GameData.UnitsInEachChapter

            For i As Integer = 0 To chapterPointers.Length - 1
                Dim pointer = chapterPointers(i)
                Dim unitCount = chapterUnitCounts.Item(i)
                Dim unitList As ArrayList = New ArrayList()
                fileReader.Seek(pointer, IO.SeekOrigin.Begin)
                For j As Integer = 1 To unitCount
                    Dim unit As FEChapterUnit = New FEChapterUnit
                    unit.initializeWithBytesFromOffset(fileReader, fileReader.Position, FE6GameData.ChapterUnitEntrySize, type)
                    unitList.Add(unit)
                Next
                chapterUnitData.Add(unitList)
            Next

            lordCharacters = FE6GameData.lordCharacterIDs()
            thiefCharacters = FE6GameData.thiefCharacterIDs()
            bossCharacters = FE6GameData.bossCharacterIDs()
            exemptCharacters = FE6GameData.exemptCharacterIDs()

        ElseIf type = Utilities.GameType.GameTypeFE7 Then

            fileReader.Seek(FE7GameData.PointerToCharacterTableOffset, IO.SeekOrigin.Begin)
            characterTableOffset = Utilities.ReadWord(fileReader, True)
            If characterTableOffset <> FE7GameData.DefaultCharacterTableOffset Then
                MsgBox("Character Table Offset has been updated. Character Table may have been repointed." + vbCrLf _
                       & "If this is a hacked game, not all characters may be randomized if the table is expanded." + vbCrLf _
                       & "Determining which units are enemies and allies may also be inaccurate.", MsgBoxStyle.Information, "Notice")
                repointedCharacterTable = True
            End If
            numberOfCharacters = FE7GameData.CharacterCount
            characterEntrySize = FE7GameData.CharacterEntrySize

            fileReader.Seek(FE7GameData.PointerToClassTableOffset, IO.SeekOrigin.Begin)
            classTableOffset = Utilities.ReadWord(fileReader, True)
            If classTableOffset <> FE7GameData.DefaultClassTableOffset Then
                MsgBox("Class Table Offset has been updated. Class Table may have been repointed." + vbCrLf _
                       & "If the number of classes has changed, only the classes up to the original amount" + vbCrLf _
                       & "will be updated and used.", MsgBoxStyle.Information, "Notice")
                repointedClassTable = True
            End If
            numberOfClasses = FE7GameData.ClassCount
            classEntrySize = FE7GameData.ClassEntrySize

            fileReader.Seek(FE7GameData.PointerToItemTableOffset, IO.SeekOrigin.Begin)
            itemTableOffset = Utilities.ReadWord(fileReader, True)
            If itemTableOffset <> FE7GameData.DefaultItemTableOffset Then
                MsgBox("Item Table Offset has been updated. Item Table may have been repointed." + vbCrLf _
                       & "If the number of items has changed, only the items up to the original amount" + vbCrLf _
                       & "will be updated and used.", MsgBoxStyle.Information, "Notice")
                repointedItemTable = True
            End If
            numberOfItems = FE7GameData.ItemCount
            itemEntrySize = FE7GameData.ItemEntrySize

            MsgBox("You seem to be randomizing FE7. Note that depending on the randomizing results, the tutorial (Lyn Normal Mode) may no longer be completable." + vbCrLf + vbCrLf _
                   & "It is recommended that you only play this on the Eliwood and Hector Modes to ensure functionality.", MsgBoxStyle.Information, "Notice")

        ElseIf type = Utilities.GameType.GameTypeFE8 Then

            fileReader.Seek(FE8GameData.PointerToCharacterTableOffset, IO.SeekOrigin.Begin)
            characterTableOffset = Utilities.ReadWord(fileReader, True)
            If characterTableOffset <> FE8GameData.DefaultCharacterTableOffset Then
                MsgBox("Character Table Offset has been updated. Character Table may have been repointed." + vbCrLf _
                       & "If this is a hacked game, not all characters may be randomized if the table is expanded." + vbCrLf _
                       & "Determining which units are enemies and allies may also be inaccurate.", MsgBoxStyle.Information, "Notice")
                repointedCharacterTable = True
            End If
            numberOfCharacters = FE8GameData.CharacterCount
            characterEntrySize = FE8GameData.CharacterEntrySize

            fileReader.Seek(FE8GameData.PointerToClassTableOffset, IO.SeekOrigin.Begin)
            classTableOffset = Utilities.ReadWord(fileReader, True)
            If classTableOffset <> FE8GameData.DefaultClassTableOffset Then
                MsgBox("Class Table Offset has been updated. Class Table may have been repointed." + vbCrLf _
                       & "If the number of classes has changed, only the classes up to the original amount" + vbCrLf _
                       & "will be updated and used.", MsgBoxStyle.Information, "Notice")
                repointedClassTable = True
            End If
            numberOfClasses = FE8GameData.ClassCount
            classEntrySize = FE8GameData.ClassEntrySize

            fileReader.Seek(FE8GameData.PointerToItemTableOffset, IO.SeekOrigin.Begin)
            itemTableOffset = Utilities.ReadWord(fileReader, True)
            If itemTableOffset <> FE8GameData.DefaultItemTableOffset Then
                MsgBox("Item Table Offset has been updated. Item Table may have been repointed." + vbCrLf _
                       & "If the number of items has changed, only the items up to the original amount" + vbCrLf _
                       & "will be updated and used.", MsgBoxStyle.Information, "Notice")
                repointedItemTable = True
            End If
            numberOfItems = FE8GameData.ItemCount
            itemEntrySize = FE8GameData.ItemEntrySize

            MsgBox("You seem to be randomizing FE8. Note that depending on the randomizing results, the tutorial (Easy Mode) may no longer be completable." + vbCrLf + vbCrLf _
                   & "It is recommended that you only play this on the Normal Or Difficult Modes to ensure functionality.", MsgBoxStyle.Information, "Notice")
        End If

        fileReader.Seek(characterTableOffset, IO.SeekOrigin.Begin)

        For index As Integer = 1 To numberOfCharacters
            Dim currentCharacter As FECharacter = New FECharacter

            currentCharacter.initializeWithBytesFromOffset(fileReader, fileReader.Position, characterEntrySize)

            characterList.Add(currentCharacter)
            Utilities.setObjectForKey(characterLookup, currentCharacter, currentCharacter.characterId)
        Next

        fileReader.Seek(classTableOffset, IO.SeekOrigin.Begin)

        For index As Integer = 1 To numberOfClasses
            Dim currentClass As FEClass = New FEClass

            currentClass.initializeWithBytesFromOffset(fileReader, fileReader.Position, classEntrySize, type)

            classList.Add(currentClass)
            Utilities.setObjectForKey(classLookup, currentClass, currentClass.classId)
        Next

        fileReader.Seek(itemTableOffset, IO.SeekOrigin.Begin)

        For index As Integer = 1 To numberOfItems
            Dim currentItem As FEItem = New FEItem

            currentItem.initializeWithBytesFromOffset(fileReader, fileReader.Position, itemEntrySize, type)

            itemList.Add(currentItem)
            Utilities.setObjectForKey(itemLookup, currentItem, currentItem.weaponID)
        Next

        fileReader.Close()

        ' For easier lookup later, also organize items by their type and rank.
        Dim itemByType As Hashtable = New Hashtable()
        Dim itemByRank As Hashtable = New Hashtable()
        For Each itemEntry As FEItem In itemList
            ' The list starts with a terminal 0 item, skip that one.
            If itemEntry.weaponID = 0 Then Continue For

            Dim weaponType As FEItem.WeaponType = itemEntry.type
            Dim weaponList As ArrayList = itemByType.Item(weaponType)
            If IsNothing(weaponList) Then
                weaponList = New ArrayList()
                itemByType.Add(weaponType, weaponList)
            End If
            weaponList.Add(itemEntry.weaponID)

            Dim weaponRank = itemEntry.rank
            Dim rankList As ArrayList = itemByRank.Item(weaponRank)
            If IsNothing(rankList) Then
                rankList = New ArrayList()
                itemByRank.Add(weaponRank, rankList)
            End If
            rankList.Add(itemEntry.weaponID)
        Next

        Dim rng As Random = New Random

        For Each character As FECharacter In characterList
            If shouldRandomizeBases Then
                character.randomizeBases(baseVariance, rng)
            End If

            If shouldRandomizeGrowths Then
                character.randomizeGrowths(growthsVariance, shouldForceMinimumGrowth, shouldWeightHPGrowths, rng)
            End If

            If shouldRandomizeCON Then
                character.randomizeCON(minimumCON, classLookup.Item(character.classId), rng)
            End If

            If shouldRandomizeAffinity Then
                character.randomizeAffinity(rng)
            End If
        Next

        If shouldRandomizeMOV Then
            For Each charClass As FEClass In classList
                charClass.randomizeMOV(minimumMOV, maximumMOV, rng)
            Next
        End If

        If shouldRandomizeItems Then
            For Each item As FEItem In itemList
                item.randomizeItemDurability(durabilityVariance, minimumDurability, rng)
                item.randomizeItemMight(mightVariance, minimumMight, rng)
                item.randomizeItemHit(hitVariance, minimumHit, rng)
                item.randomizeItemWeight(weightVariance, minimumWeight, maximumWeight, rng)
                item.randomizeItemCritical(criticalVariance, minimumCritical, rng)
                If randomTraits Then
                    item.assignRandomEffect(rng, type)
                End If
            Next
        End If

        If shouldRandomizeClasses Then
            ' Cache results as they happen so that we maintain consistency.
            Dim targetClasses As Hashtable = New Hashtable()
            Dim targetInventory As Hashtable = New Hashtable()

            Dim importantCharacterIDs = New ArrayList(System.Enum.GetValues(GetType(FE6GameData.CharacterList)))

            Dim weaponLevelIncreaseChance As Integer = 0

            ' Go through each chapter.
            For Each chapterUnits As ArrayList In chapterUnitData
                ' Let's start with 5% per chapter (Gaidens and splits included)
                weaponLevelIncreaseChance += 8
                ' For every unit found in the chapter data
                For Each unit As FEChapterUnit In chapterUnits
                    ' If it's a real character (i.e. not a random mook) then see if we already had something for
                    ' him or her.
                    Dim characterIDObject As Object = System.Enum.ToObject(GetType(FE6GameData.CharacterList), unit.characterId)
                    If importantCharacterIDs.Contains(characterIDObject) Then
                        ' If we do, go ahead and use what we've stored if we have it.
                        If targetClasses.ContainsKey(characterIDObject) Then
                            Dim targetClassId = targetClasses.Item(characterIDObject)
                            unit.classId = targetClassId
                            Dim targetInventoryList As ArrayList = targetInventory.Item(characterIDObject)
                            unit.item1Id = targetInventoryList.Item(0)
                            unit.item2Id = targetInventoryList.Item(1)
                            unit.item3Id = targetInventoryList.Item(2)
                            unit.item4Id = targetInventoryList.Item(3)
                        Else
                            ' First time this character shows up. Figure out what he should be.
                            ' If he's one of the units we shouldn't touch, then skip it.
                            If Not randomLords And lordCharacters.Contains(characterIDObject) Then
                                Continue For
                            ElseIf Not randomThieves And thiefCharacters.Contains(characterIDObject) Then
                                Continue For
                            ElseIf Not randomBosses And bossCharacters.Contains(characterIDObject) Then
                                Continue For
                            ElseIf exemptCharacters.Contains(characterIDObject) Then
                                Continue For
                            End If

                            ' If we got this far, he/she's getting randomized
                            ' Grab a random class from the allowed classes. Each game
                            ' will know what valid classes are possible.
                            Dim newClassId As Byte
                            ' Keep track of whether this character was a Lord character, because whatever
                            ' his or her new class is also needs to have lord abilities (like Seize)
                            Dim wasLord As Boolean = False
                            ' Keep track of thieves too. If they're randomized, we need to give them
                            ' some keys.
                            Dim wasThief As Boolean = False
                            Dim currentCharacter As FECharacter = characterLookup.Item(unit.characterId)
                            If type = Utilities.GameType.GameTypeFE6 Then
                                ' FE6 stores this in the character object.
                                Dim oldClassIDObject As FE6GameData.ClassList = System.Enum.ToObject(GetType(FE6GameData.ClassList), currentCharacter.classId)
                                Dim oldClass As FEClass = classLookup.Item(currentCharacter.classId)
                                wasLord = oldClass.isLord
                                wasThief = oldClass.isThief
                                newClassId = FE6GameData.randomClassFromOriginalClass(oldClassIDObject, randomLords, randomThieves, uniqueClasses, rng)
                            End If

                            Dim newClass As FEClass = classLookup.Item(newClassId)

                            ' If the old class was a lord (i.e. this is a lord character)
                            ' then his/her new class also needs lord privileges (ability to Seize)
                            If wasLord Then
                                newClass.ability2 = newClass.ability2 Or FEClass.ClassAbility2.Lord
                            End If

                            ' Update the character's class in the object.
                            currentCharacter.classId = newClass.classId

                            ' Make sure their weapon levels are consistent with their new class.
                            currentCharacter.swordLevel = newClass.swordLevel
                            currentCharacter.spearLevel = newClass.spearLevel
                            currentCharacter.axeLevel = newClass.axeLevel
                            currentCharacter.bowLevel = newClass.bowLevel
                            currentCharacter.staffLevel = newClass.staffLevel
                            currentCharacter.animaLevel = newClass.animaLevel
                            currentCharacter.darkLevel = newClass.darkLevel
                            currentCharacter.lightLevel = newClass.lightLevel

                            ' Add a chance to increase their weapon ranks depending
                            ' on how late they join.
                            currentCharacter.increaseWeaponRanksWithPercentChance(weaponLevelIncreaseChance, type, rng)

                            ' Cache the result of this character's class.
                            targetClasses.Add(characterIDObject, newClass.classId)

                            ' Remember to update their class in the chapter data.
                            unit.classId = newClass.classId

                            ' Validate their inventory too.
                            ' Check each item and make sure weapons they start with are usable.
                            ' Replace ones that aren't with ones that are.
                            Dim validatedInventory As ArrayList = New ArrayList()

                            Dim validWeapons As ArrayList = New ArrayList()
                            If currentCharacter.swordLevel > 0 Then
                                Dim validSwords As ArrayList = itemByType.Item(FEItem.WeaponType.WeaponTypeSword)
                                For Each key In itemByRank.Keys
                                    If key <= currentCharacter.swordLevel Then
                                        Dim weaponsByRank As ArrayList = itemByRank(key)
                                        validWeapons = Utilities.arrayUnion(validWeapons, Utilities.arrayIntersect(validSwords, weaponsByRank))
                                    End If
                                Next
                            End If
                            If currentCharacter.spearLevel > 0 Then
                                Dim validSpears As ArrayList = itemByType.Item(FEItem.WeaponType.WeaponTypeSpear)
                                For Each key In itemByRank.Keys
                                    If key <= currentCharacter.spearLevel Then
                                        Dim weaponsByRank As ArrayList = itemByRank(key)
                                        validWeapons = Utilities.arrayUnion(validWeapons, Utilities.arrayIntersect(validSpears, weaponsByRank))
                                    End If
                                Next
                            End If
                            If currentCharacter.axeLevel > 0 Then
                                Dim validAxes As ArrayList = itemByType.Item(FEItem.WeaponType.WeaponTypeAxe)
                                For Each key In itemByRank.Keys
                                    If key <= currentCharacter.axeLevel Then
                                        Dim weaponsByRank As ArrayList = itemByRank(key)
                                        validWeapons = Utilities.arrayUnion(validWeapons, Utilities.arrayIntersect(validAxes, weaponsByRank))
                                    End If
                                Next
                            End If
                            If currentCharacter.bowLevel > 0 Then
                                Dim validBows As ArrayList = itemByType.Item(FEItem.WeaponType.WeaponTypeBow)
                                For Each key In itemByRank.Keys
                                    If key <= currentCharacter.bowLevel Then
                                        Dim weaponsByRank As ArrayList = itemByRank(key)
                                        validWeapons = Utilities.arrayUnion(validWeapons, Utilities.arrayIntersect(validBows, weaponsByRank))
                                    End If
                                Next
                            End If
                            If currentCharacter.animaLevel > 0 Then
                                Dim validAnima As ArrayList = itemByType.Item(FEItem.WeaponType.WeaponTypeAnima)
                                For Each key In itemByRank.Keys
                                    If key <= currentCharacter.animaLevel Then
                                        Dim weaponsByRank As ArrayList = itemByRank(key)
                                        validWeapons = Utilities.arrayUnion(validWeapons, Utilities.arrayIntersect(validAnima, weaponsByRank))
                                    End If
                                Next
                            End If
                            If currentCharacter.lightLevel > 0 Then
                                Dim validLight As ArrayList = itemByType.Item(FEItem.WeaponType.WeaponTypeLight)
                                For Each key In itemByRank.Keys
                                    If key < currentCharacter.lightLevel Then
                                        Dim weaponsByRank As ArrayList = itemByRank(key)
                                        validWeapons = Utilities.arrayUnion(validWeapons, Utilities.arrayIntersect(validLight, weaponsByRank))
                                    End If
                                Next
                            End If
                            If currentCharacter.darkLevel > 0 Then
                                Dim validDark As ArrayList = itemByType.Item(FEItem.WeaponType.WeaponTypeDark)
                                For Each key In itemByRank.Keys
                                    If key <= currentCharacter.darkLevel Then
                                        Dim weaponsByRank As ArrayList = itemByRank(key)
                                        validWeapons = Utilities.arrayUnion(validWeapons, Utilities.arrayIntersect(validDark, weaponsByRank))
                                    End If
                                Next
                            End If
                            If currentCharacter.staffLevel > 0 Then
                                Dim validStaves As ArrayList = itemByType.Item(FEItem.WeaponType.WeaponTypeStaff)
                                For Each key In itemByRank.Keys
                                    If key <= currentCharacter.staffLevel Then
                                        Dim weaponsByRank As ArrayList = itemByRank(key)
                                        validWeapons = Utilities.arrayUnion(validWeapons, Utilities.arrayIntersect(validStaves, weaponsByRank))
                                    End If
                                Next
                            End If

                            If unit.item1Id <> 0 Then
                                Dim item1 As FEItem = itemLookup.Item(unit.item1Id)
                                If IsNothing(item1) Then
                                    ' Not an item we know of (probably not of the default set)
                                    ' Just leave it as is.
                                    validatedInventory.Add(unit.item1Id)
                                ElseIf item1.isWeapon() Or item1.type = FEItem.WeaponType.WeaponTypeStaff Then
                                    ' It's a weapon we know. Check if the character can still use it.
                                    If Utilities.canCharacterUseWeapon(currentCharacter, item1, newClass) Then
                                        ' If so, leave the weapon in.
                                        validatedInventory.Add(item1.weaponID)
                                    Else
                                        ' It's a weapon the character can't use. Need
                                        ' to find a suitable replacement.
                                        If validWeapons.Count > 0 Then
                                            Dim newWeapon As FEItem = Nothing
                                            Do While Not Utilities.canCharacterUseWeapon(currentCharacter, newWeapon, newClass)
                                                newWeapon = itemLookup.Item(validWeapons.Item(rng.Next(validWeapons.Count)))
                                            Loop
                                            validatedInventory.Add(newWeapon.weaponID)
                                        ElseIf newClass.classId = FE6GameData.ClassList.Manakete Then
                                            validatedInventory.Add(FE6GameData.ItemList.FireDragonStone)
                                        ElseIf newClass.classId = FE6GameData.ClassList.Manakete_F Then
                                            validatedInventory.Add(FE6GameData.ItemList.DivineDragonStone)
                                        Else
                                            validatedInventory.Add(item1.weaponID)
                                        End If
                                    End If
                                Else
                                    ' It's not a weapon to begin with.
                                    ' Leave it as is.
                                    validatedInventory.Add(item1.weaponID)
                                End If
                            Else
                                validatedInventory.Add(0)
                            End If
                            If unit.item2Id <> 0 Then
                                Dim item2 As FEItem = itemLookup.Item(unit.item2Id)
                                If IsNothing(item2) Then
                                    ' Not an item we know of (probably not of the default set)
                                    ' Just leave it as is.
                                    validatedInventory.Add(unit.item2Id)
                                ElseIf item2.isWeapon() Or item2.type = FEItem.WeaponType.WeaponTypeStaff Then
                                    ' It's a weapon we know. Check if the character can still use it.
                                    If Utilities.canCharacterUseWeapon(currentCharacter, item2, newClass) Then
                                        ' If so, leave the weapon in.
                                        validatedInventory.Add(item2.weaponID)
                                    Else
                                        ' It's a weapon the character can't use. Need
                                        ' to find a suitable replacement.
                                        If validWeapons.Count > 0 Then
                                            Dim newWeapon As FEItem = Nothing
                                            Do While Not Utilities.canCharacterUseWeapon(currentCharacter, newWeapon, newClass)
                                                newWeapon = itemLookup.Item(validWeapons.Item(rng.Next(validWeapons.Count)))
                                            Loop
                                            validatedInventory.Add(newWeapon.weaponID)
                                        ElseIf newClass.classId = FE6GameData.ClassList.Manakete Then
                                            validatedInventory.Add(FE6GameData.ItemList.FireDragonStone)
                                        ElseIf newClass.classId = FE6GameData.ClassList.Manakete_F Then
                                            validatedInventory.Add(FE6GameData.ItemList.DivineDragonStone)
                                        Else
                                            validatedInventory.Add(item2.weaponID)
                                        End If
                                    End If
                                Else
                                    ' It's not a weapon to begin with.
                                    ' Leave it as is.
                                    validatedInventory.Add(item2.weaponID)
                                End If
                            Else
                                validatedInventory.Add(0)
                            End If
                            If unit.item3Id <> 0 Then
                                Dim item3 As FEItem = itemLookup.Item(unit.item3Id)
                                If IsNothing(item3) Then
                                    ' Not an item we know of (probably not of the default set)
                                    ' Just leave it as is.
                                    validatedInventory.Add(unit.item3Id)
                                ElseIf item3.isWeapon() Or item3.type = FEItem.WeaponType.WeaponTypeStaff Then
                                    ' It's a weapon we know. Check if the character can still use it.
                                    If Utilities.canCharacterUseWeapon(currentCharacter, item3, newClass) Then
                                        ' If so, leave the weapon in.
                                        validatedInventory.Add(item3.weaponID)
                                    Else
                                        ' It's a weapon the character can't use. Need
                                        ' to find a suitable replacement.
                                        If validWeapons.Count > 0 Then
                                            Dim newWeapon As FEItem = Nothing
                                            Do While Not Utilities.canCharacterUseWeapon(currentCharacter, newWeapon, newClass)
                                                newWeapon = itemLookup.Item(validWeapons.Item(rng.Next(validWeapons.Count)))
                                            Loop
                                            validatedInventory.Add(newWeapon.weaponID)
                                        ElseIf newClass.classId = FE6GameData.ClassList.Manakete Then
                                            validatedInventory.Add(FE6GameData.ItemList.FireDragonStone)
                                        ElseIf newClass.classId = FE6GameData.ClassList.Manakete_F Then
                                            validatedInventory.Add(FE6GameData.ItemList.DivineDragonStone)
                                        Else
                                            validatedInventory.Add(item3.weaponID)
                                        End If
                                    End If
                                Else
                                    ' It's not a weapon to begin with.
                                    ' Leave it as is.
                                    validatedInventory.Add(item3.weaponID)
                                End If
                            Else
                                validatedInventory.Add(0)
                            End If
                            If unit.item4Id <> 0 Then
                                Dim item4 As FEItem = itemLookup.Item(unit.item4Id)
                                If IsNothing(item4) Then
                                    ' Not an item we know of (probably not of the default set)
                                    ' Just leave it as is.
                                    validatedInventory.Add(unit.item4Id)
                                ElseIf item4.isWeapon() Or item4.type = FEItem.WeaponType.WeaponTypeStaff Then
                                    ' It's a weapon we know. Check if the character can still use it.
                                    If Utilities.canCharacterUseWeapon(currentCharacter, item4, newClass) Then
                                        ' If so, leave the weapon in.
                                        validatedInventory.Add(item4.weaponID)
                                    Else
                                        ' It's a weapon the character can't use. Need
                                        ' to find a suitable replacement.
                                        If validWeapons.Count > 0 Then
                                            Dim newWeapon As FEItem = Nothing
                                            Do While Not Utilities.canCharacterUseWeapon(currentCharacter, newWeapon, newClass)
                                                newWeapon = itemLookup.Item(validWeapons.Item(rng.Next(validWeapons.Count)))
                                            Loop
                                            validatedInventory.Add(newWeapon.weaponID)
                                        ElseIf newClass.classId = FE6GameData.ClassList.Manakete Then
                                            validatedInventory.Add(FE6GameData.ItemList.FireDragonStone)
                                        ElseIf newClass.classId = FE6GameData.ClassList.Manakete_F Then
                                            validatedInventory.Add(FE6GameData.ItemList.DivineDragonStone)
                                        Else
                                            validatedInventory.Add(item4.weaponID)
                                        End If
                                    End If
                                Else
                                    ' It's not a weapon to begin with.
                                    ' Leave it as is.
                                    validatedInventory.Add(item4.weaponID)
                                End If
                            Else
                                validatedInventory.Add(0)
                            End If

                            ' For thieves (either characters originally thieves or
                            ' characters that are turned into thieves), they have special
                            ' requirements to make sure the game is still completable.
                            ' Old thieves need to have keys to make sure they can perform basic
                            ' thief actions. New thieves get a free set of lockpicks.

                            If randomThieves Then
                                If wasThief And Not newClass.isThief Then
                                    Dim lockpickIndex As Integer = validatedInventory.IndexOf(Convert.ToByte(FE6GameData.ItemList.Lockpick))
                                    If lockpickIndex <> -1 Then
                                        validatedInventory.RemoveAt(lockpickIndex)
                                        validatedInventory.Add(0)
                                    End If

                                    Dim equipment As ArrayList = FE6GameData.randomizedFromThiefEquipment
                                    For Each item As Byte In equipment
                                        Dim firstEmptySlot As Integer = validatedInventory.IndexOf(0)
                                        If firstEmptySlot = -1 Then
                                            Exit For
                                        End If
                                        validatedInventory.Insert(firstEmptySlot, item)
                                    Next
                                ElseIf Not wasThief And newClass.isThief Then
                                    Dim equipment As ArrayList = FE6GameData.randomizedToThiefEquipment
                                    For Each item As Byte In equipment
                                        Dim firstEmptySlot As Integer = validatedInventory.IndexOf(0)
                                        If firstEmptySlot = -1 Then
                                            Exit For
                                        End If
                                        validatedInventory.Insert(firstEmptySlot, item)
                                    Next
                                End If
                            End If

                            While validatedInventory.Count > 4
                                validatedInventory.RemoveAt(validatedInventory.Count - 1)
                            End While

                            unit.item1Id = validatedInventory.Item(0)
                            unit.item2Id = validatedInventory.Item(1)
                            unit.item3Id = validatedInventory.Item(2)
                            unit.item4Id = validatedInventory.Item(3)

                            targetInventory.Add(characterIDObject, validatedInventory)
                        End If
                    End If
                Next
            Next
        End If

        ' Write it all back to disk.
        Dim fileWriter = IO.File.OpenWrite(OpenFileDialog1.FileName)

        fileWriter.Seek(characterTableOffset, IO.SeekOrigin.Begin)

        For index As Integer = 0 To characterList.Count - 1
            Dim character As FECharacter = characterList.Item(index)
            character.writeStatsToCharacterStartingAtOffset(fileWriter, fileWriter.Position, characterEntrySize)
        Next

        fileWriter.Seek(classTableOffset, IO.SeekOrigin.Begin)

        For index As Integer = 0 To classList.Count - 1
            Dim currentClass As FEClass = classList.Item(index)
            currentClass.writeClassStartingAtOffset(fileWriter, fileWriter.Position, classEntrySize, type)
        Next

        fileWriter.Seek(itemTableOffset, IO.SeekOrigin.Begin)

        For index As Integer = 0 To itemList.Count - 1
            Dim currentItem As FEItem = itemList.Item(index)
            currentItem.writeItemToOffset(fileWriter, fileWriter.Position, itemEntrySize, type)
        Next

        If type = Utilities.GameType.GameTypeFE6 Then

            Dim chapterPointers As Array = System.Enum.GetValues(GetType(FE6GameData.ChapterUnitReference))
            Dim chapterUnitCounts As ArrayList = FE6GameData.UnitsInEachChapter()

            For i As Integer = 0 To chapterPointers.Length - 1
                Dim pointer = chapterPointers(i)
                Dim unitCount = chapterUnitCounts.Item(i)
                fileWriter.Seek(pointer, IO.SeekOrigin.Begin)
                Dim unitList As ArrayList = chapterUnitData.Item(i)
                For j As Integer = 0 To unitList.Count - 1
                    Dim unit As FEChapterUnit = unitList.Item(j)
                    unit.writeChapterUnitToOffset(fileWriter, fileWriter.Position, FE6GameData.ChapterUnitEntrySize, Utilities.GameType.GameTypeFE6)
                Next
            Next
        End If

        fileWriter.Close()

        MsgBox("Randomized!", MsgBoxStyle.OkOnly, "Stats Shuffler")

        reenableAllControls()

    End Sub

    Private Sub disableAllControls()
        BrowseButton.Enabled = False

        RandomizeGrowthsToggle.Enabled = False
        GrowthVarianceControl.Enabled = False
        MinimumGrowthToggle.Enabled = False
        WeightedHPGrowthsToggle.Enabled = False

        RandomizeBasesToggle.Enabled = False
        BaseVarianceControl.Enabled = False
        RandomizeCONToggle.Enabled = False
        MinimumCONControl.Enabled = False
        RandomizeMOVToggle.Enabled = False
        MinimumMOVControl.Enabled = False
        MaximumMOVControl.Enabled = False

        RandomizeAffinityToggle.Enabled = False

        RandomizeAffinityBonusToggle.Enabled = False
        AffinityBonusAmountControl.Enabled = False

        RandomizeClassesToggle.Enabled = False
        IncludeLordsToggle.Enabled = False
        IncludeThievesToggle.Enabled = False
        IncludeBossesToggle.Enabled = False
        AllowUniqueClassesToggle.Enabled = False

        RandomizeItemsToggle.Enabled = False
        MightVarianceControl.Enabled = False
        MinimumMightControl.Enabled = False
        HitVarianceControl.Enabled = False
        MinimumHitControl.Enabled = False
        CriticalVarianceControl.Enabled = False
        MinimumCriticalControl.Enabled = False
        WeightVarianceControl.Enabled = False
        MinimumWeightControl.Enabled = False
        MaximumWeightControl.Enabled = False
        MinimumDurabilityControl.Enabled = False
        DurabilityVarianceControl.Enabled = False
        AllowRandomTraitsToggle.Enabled = False

        IncreaseEnemyGrowthsToggle.Enabled = False
        EnemyBuffControl.Enabled = False
        SetMinimumEnemyBuffControl.Enabled = False
        SetMaximumEnemyBuffControl.Enabled = False
        SetConstantEnemyBuffControl.Enabled = False
        BuffBossesToggle.Enabled = False

        NormalRecruitmentOption.Enabled = False
        ReverseRecruitmentOption.Enabled = False
        RandomRecruitmentOption.Enabled = False

        RandomizeButton.Enabled = False
    End Sub

    Private Sub reenableAllControls()
        BrowseButton.Enabled = True

        RandomizeGrowthsToggle.Enabled = True
        GrowthVarianceControl.Enabled = RandomizeGrowthsToggle.Checked
        MinimumGrowthToggle.Enabled = RandomizeGrowthsToggle.Checked
        WeightedHPGrowthsToggle.Enabled = RandomizeGrowthsToggle.Checked

        RandomizeBasesToggle.Enabled = True
        BaseVarianceControl.Enabled = RandomizeBasesToggle.Checked

        RandomizeCONToggle.Enabled = True
        MinimumCONControl.Enabled = RandomizeCONToggle.Checked

        RandomizeMOVToggle.Enabled = True
        MinimumMOVControl.Enabled = RandomizeMOVToggle.Checked
        MaximumMOVControl.Enabled = RandomizeMOVToggle.Checked

        RandomizeAffinityToggle.Enabled = True

        ' Not supported yet.
        If False Then
            RandomizeAffinityBonusToggle.Enabled = True
            AffinityBonusAmountControl.Enabled = RandomizeAffinityBonusToggle.Checked
        End If

        If type = Utilities.GameType.GameTypeFE6 Then
            RandomizeClassesToggle.Enabled = True
            IncludeLordsToggle.Enabled = RandomizeClassesToggle.Checked
            IncludeThievesToggle.Enabled = RandomizeClassesToggle.Checked
            IncludeBossesToggle.Enabled = RandomizeClassesToggle.Checked
            AllowUniqueClassesToggle.Enabled = RandomizeClassesToggle.Checked
        End If

        RandomizeItemsToggle.Enabled = True
        MightVarianceControl.Enabled = RandomizeItemsToggle.Checked
        MinimumMightControl.Enabled = RandomizeItemsToggle.Checked
        HitVarianceControl.Enabled = RandomizeItemsToggle.Checked
        MinimumHitControl.Enabled = RandomizeItemsToggle.Checked
        CriticalVarianceControl.Enabled = RandomizeItemsToggle.Checked
        MinimumCriticalControl.Enabled = RandomizeItemsToggle.Checked
        WeightVarianceControl.Enabled = RandomizeItemsToggle.Checked
        MinimumWeightControl.Enabled = RandomizeItemsToggle.Checked
        MaximumWeightControl.Enabled = RandomizeItemsToggle.Checked
        MinimumDurabilityControl.Enabled = RandomizeItemsToggle.Checked
        DurabilityVarianceControl.Enabled = RandomizeItemsToggle.Checked
        AllowRandomTraitsToggle.Enabled = RandomizeItemsToggle.Checked

        ' Not supported yet.
        If False Then
            IncreaseEnemyGrowthsToggle.Enabled = True
            EnemyBuffControl.Enabled = IncreaseEnemyGrowthsToggle.Checked
            SetMinimumEnemyBuffControl.Enabled = IncreaseEnemyGrowthsToggle.Checked
            SetMaximumEnemyBuffControl.Enabled = IncreaseEnemyGrowthsToggle.Checked
            SetConstantEnemyBuffControl.Enabled = IncreaseEnemyGrowthsToggle.Checked
            BuffBossesToggle.Enabled = IncreaseEnemyGrowthsToggle.Checked
        End If

        ' If type = Utilities.GameType.GameTypeFE6 Then
        ' Not supported yet
        If False Then
            NormalRecruitmentOption.Enabled = True
            ReverseRecruitmentOption.Enabled = True
            RandomRecruitmentOption.Enabled = True
        End If

        RandomizeButton.Enabled = type <> Utilities.GameType.GameTypeUnknown
    End Sub

    Private Sub RandomizeGrowthsToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RandomizeGrowthsToggle.CheckedChanged
        shouldRandomizeGrowths = RandomizeGrowthsToggle.Checked

        GrowthVarianceControl.Enabled = shouldRandomizeGrowths
        MinimumGrowthToggle.Enabled = shouldRandomizeGrowths
        WeightedHPGrowthsToggle.Enabled = shouldRandomizeGrowths
    End Sub

    Private Sub GrowthVarianceControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GrowthVarianceControl.ValueChanged
        Dim currentValue = GrowthVarianceControl.Value
        If currentValue Mod 5 <> 0 Then
            currentValue = currentValue + (5 - (currentValue Mod 5))
        End If
        GrowthVarianceControl.Value = currentValue
        growthsVariance = currentValue
    End Sub

    Private Sub MinimumGrowthToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MinimumGrowthToggle.CheckedChanged
        shouldForceMinimumGrowth = MinimumGrowthToggle.Checked
    End Sub

    Private Sub WeightedHPGrowthsToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles WeightedHPGrowthsToggle.CheckedChanged
        shouldWeightHPGrowths = WeightedHPGrowthsToggle.Checked
    End Sub

    Private Sub RandomizeBasesToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RandomizeBasesToggle.CheckedChanged
        shouldRandomizeBases = RandomizeBasesToggle.Checked

        BaseVarianceControl.Enabled = shouldRandomizeBases
    End Sub

    Private Sub BaseVarianceControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles BaseVarianceControl.ValueChanged
        baseVariance = BaseVarianceControl.Value
    End Sub

    Private Sub RandomizeCONToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RandomizeCONToggle.CheckedChanged
        shouldRandomizeCON = RandomizeCONToggle.Checked
        MinimumCONControl.Enabled = shouldRandomizeCON
    End Sub

    Private Sub RandomizeMOVToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RandomizeMOVToggle.CheckedChanged
        shouldRandomizeMOV = RandomizeMOVToggle.Checked
        MinimumMOVControl.Enabled = shouldRandomizeMOV
        MaximumMOVControl.Enabled = shouldRandomizeMOV
    End Sub

    Private Sub MinimumMOVControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MinimumMOVControl.ValueChanged
        Dim targetMOV = MinimumMOVControl.Value
        If targetMOV > maximumMOV Then
            targetMOV = maximumMOV
        End If
        minimumMOV = targetMOV
        MinimumMOVControl.Value = targetMOV
        MaximumMOVControl.Minimum = targetMOV
    End Sub

    Private Sub MaximumMOVControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MaximumMOVControl.ValueChanged
        Dim targetMOV = MaximumMOVControl.Value
        If targetMOV < minimumMOV Then
            targetMOV = minimumMOV
        End If
        maximumMOV = targetMOV
        MaximumMOVControl.Value = targetMOV
        MinimumMOVControl.Maximum = targetMOV
    End Sub

    Private Sub RandomizeAffinityToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RandomizeAffinityToggle.CheckedChanged
        shouldRandomizeAffinity = RandomizeAffinityToggle.Checked
    End Sub

    Private Sub RandomizeAffinityBonusToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RandomizeAffinityBonusToggle.CheckedChanged
        shouldRandomizeAffinityBonuses = RandomizeAffinityBonusToggle.Checked

        AffinityBonusAmountControl.Enabled = shouldRandomizeAffinityBonuses
    End Sub

    Private Sub AffinityBonusAmountControl_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AffinityBonusAmountControl.SelectedIndexChanged
        affinityBonusMaxMultiplier = AffinityBonusAmountControl.SelectedIndex + 1
    End Sub

    Private Sub RandomizeItemsToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RandomizeItemsToggle.CheckedChanged
        shouldRandomizeItems = RandomizeItemsToggle.Checked

        MightVarianceControl.Enabled = shouldRandomizeItems
        MinimumMightControl.Enabled = shouldRandomizeItems
        HitVarianceControl.Enabled = shouldRandomizeItems
        MinimumHitControl.Enabled = shouldRandomizeItems
        CriticalVarianceControl.Enabled = shouldRandomizeItems
        MinimumCriticalControl.Enabled = shouldRandomizeItems
        WeightVarianceControl.Enabled = shouldRandomizeItems
        MinimumWeightControl.Enabled = shouldRandomizeItems
        MaximumWeightControl.Enabled = shouldRandomizeItems
        MinimumDurabilityControl.Enabled = shouldRandomizeItems
        DurabilityVarianceControl.Enabled = shouldRandomizeItems
        AllowRandomTraitsToggle.Enabled = shouldRandomizeItems
    End Sub

    Private Sub MightVarianceControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MightVarianceControl.ValueChanged
        mightVariance = MightVarianceControl.Value
    End Sub

    Private Sub MinimumMightControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MinimumMightControl.ValueChanged
        minimumMight = MinimumMightControl.Value
    End Sub

    Private Sub HitVarianceControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles HitVarianceControl.ValueChanged
        Dim targetHit = HitVarianceControl.Value
        If targetHit Mod 5 <> 0 Then
            targetHit = targetHit + (5 - targetHit Mod 5)
        End If
        HitVarianceControl.Value = targetHit
        hitVariance = targetHit
    End Sub

    Private Sub MinimumHitControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MinimumHitControl.ValueChanged
        Dim targetHit = MinimumHitControl.Value
        If targetHit Mod 5 <> 0 Then
            targetHit = targetHit + (5 - targetHit Mod 5)
        End If
        minimumHit = targetHit
        MinimumHitControl.Value = targetHit
    End Sub

    Private Sub CriticalVarianceControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CriticalVarianceControl.ValueChanged
        Dim targetCrit = CriticalVarianceControl.Value
        If targetCrit Mod 5 <> 0 Then
            targetCrit = targetCrit + (5 - targetCrit Mod 5)
        End If
        criticalVariance = targetCrit
        CriticalVarianceControl.Value = targetCrit
    End Sub

    Private Sub MinimumCriticalControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MinimumCriticalControl.ValueChanged
        Dim targetCrit = MinimumCriticalControl.Value
        If targetCrit Mod 5 <> 0 Then
            targetCrit = targetCrit + (5 - targetCrit Mod 5)
        End If
        minimumCritical = targetCrit
        MinimumCriticalControl.Value = targetCrit
    End Sub

    Private Sub WeightVarianceControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles WeightVarianceControl.ValueChanged
        weightVariance = WeightVarianceControl.Value
    End Sub

    Private Sub MinimumWeightControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MinimumWeightControl.ValueChanged
        Dim targetWeight = MinimumWeightControl.Value
        If targetWeight > maximumWeight Then
            targetWeight = maximumWeight
        End If

        minimumWeight = targetWeight
        MinimumWeightControl.Value = targetWeight
        MaximumWeightControl.Minimum = targetWeight
    End Sub

    Private Sub MaximumWeightControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MaximumWeightControl.ValueChanged
        Dim targetWeight = MaximumWeightControl.Value
        If targetWeight < minimumWeight Then
            targetWeight = minimumWeight
        End If

        maximumWeight = targetWeight
        MaximumWeightControl.Value = targetWeight
        MinimumWeightControl.Maximum = targetWeight
    End Sub

    Private Sub DurabilityVarianceControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DurabilityVarianceControl.ValueChanged
        durabilityVariance = DurabilityVarianceControl.Value
    End Sub

    Private Sub MinimumDurabilityControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MinimumDurabilityControl.ValueChanged
        minimumDurability = MinimumDurabilityControl.Value
    End Sub

    Private Sub AllowRandomTraitsToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AllowRandomTraitsToggle.CheckedChanged
        randomTraits = AllowRandomTraitsToggle.Checked
    End Sub

    Private Sub RandomizeClassesToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RandomizeClassesToggle.CheckedChanged
        shouldRandomizeClasses = RandomizeClassesToggle.Checked

        IncludeLordsToggle.Enabled = shouldRandomizeClasses
        IncludeThievesToggle.Enabled = shouldRandomizeClasses
        IncludeBossesToggle.Enabled = shouldRandomizeClasses
        AllowUniqueClassesToggle.Enabled = shouldRandomizeClasses
    End Sub

    Private Sub IncludeLordsToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles IncludeLordsToggle.CheckedChanged
        randomLords = IncludeLordsToggle.Checked
    End Sub

    Private Sub IncludeThievesToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles IncludeThievesToggle.CheckedChanged
        randomThieves = IncludeThievesToggle.Checked
    End Sub

    Private Sub IncludeBossesToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles IncludeBossesToggle.CheckedChanged
        randomBosses = IncludeBossesToggle.Checked
    End Sub

    Private Sub AllowUniqueClassesToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AllowUniqueClassesToggle.CheckedChanged
        uniqueClasses = AllowUniqueClassesToggle.Checked
    End Sub

    Private Sub IncreaseEnemyGrowthsToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles IncreaseEnemyGrowthsToggle.CheckedChanged
        buffEnemies = IncreaseEnemyGrowthsToggle.Checked

        EnemyBuffControl.Enabled = buffEnemies
        SetMaximumEnemyBuffControl.Enabled = buffEnemies
        SetConstantEnemyBuffControl.Enabled = buffEnemies
        SetMinimumEnemyBuffControl.Enabled = buffEnemies
        BuffBossesToggle.Enabled = buffEnemies
    End Sub

    Private Sub EnemyBuffControl_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles EnemyBuffControl.ValueChanged
        Dim targetAmount = EnemyBuffControl.Value
        If targetAmount Mod 5 <> 0 Then
            targetAmount = targetAmount + (5 - targetAmount Mod 5)
        End If
        buffAmount = targetAmount
        EnemyBuffControl.Value = targetAmount
    End Sub

    Private Sub SetMaximumEnemyBuffControl_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SetMaximumEnemyBuffControl.CheckedChanged
        If SetMaximumEnemyBuffControl.Checked Then
            buffType = EnemyBuffType.BuffTypeSetMaximum
            SetConstantEnemyBuffControl.Checked = False
            SetMinimumEnemyBuffControl.Checked = False
        End If
    End Sub

    Private Sub SetConstantEnemyBuffControl_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SetConstantEnemyBuffControl.CheckedChanged
        If SetConstantEnemyBuffControl.Checked Then
            buffType = EnemyBuffType.BuffTypeSetConstant
            SetMinimumEnemyBuffControl.Checked = False
            SetMaximumEnemyBuffControl.Checked = False
        End If
    End Sub

    Private Sub SetMinimumEnemyBuffControl_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SetMinimumEnemyBuffControl.CheckedChanged
        If SetMinimumEnemyBuffControl.Checked Then
            buffType = EnemyBuffType.BuffTypeSetMinimum
            SetMaximumEnemyBuffControl.Checked = False
            SetConstantEnemyBuffControl.Checked = False
        End If
    End Sub

    Private Sub BuffBossesToggle_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles BuffBossesToggle.CheckedChanged
        buffBosses = BuffBossesToggle.Checked
    End Sub

    Private Sub NormalRecruitmentOption_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NormalRecruitmentOption.CheckedChanged
        If NormalRecruitmentOption.Checked Then
            recruitment = RecruitmentType.RecruitmentTypeNormal
            ReverseRecruitmentOption.Checked = False
            RandomRecruitmentOption.Checked = False
        End If
    End Sub

    Private Sub ReverseRecruitmentOption_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ReverseRecruitmentOption.CheckedChanged
        If ReverseRecruitmentOption.Checked Then
            recruitment = RecruitmentType.RecruitmentTypeReverse
            NormalRecruitmentOption.Checked = False
            RandomRecruitmentOption.Checked = False
        End If
    End Sub

    Private Sub RandomRecruitmentOption_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RandomRecruitmentOption.CheckedChanged
        If RandomRecruitmentOption.Checked Then
            recruitment = RecruitmentType.RecruitmentTypeRandom
            NormalRecruitmentOption.Checked = False
            ReverseRecruitmentOption.Checked = False
        End If
    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        disableAllControls()
        BrowseButton.Enabled = True

        ' The default min CON is 1
        minimumCON = 1
        ' The default min MOV is 1 and the default max MOV is 9
        minimumMOV = 1
        maximumMOV = 9
        ' The default multiplier if it is enabled is 1.
        affinityBonusMaxMultiplier = 1
        AffinityBonusAmountControl.SelectedIndex = 0
        ' The default weapon max weapon weight is 20 and the default min weapon weight is 1
        minimumWeight = 1
        maximumWeight = 20
        ' The default minimum durability is 1
        minimumDurability = 1

        GrowthsTooltip.SetToolTip(RandomizeGrowthsToggle, "Applies a random growth to all characters (playable and bosses). " + vbCrLf _
                    & "A character's original growths are added together and then" + vbCrLf _
                    & "redistributed across all growth areas to generate their new growths." + vbCrLf _
                    & "Redistribution is done by randomizing a stat, then randomizing an amount." + vbCrLf _
                    & "The amount is added to the running total for the stat growth. This" + vbCrLf _
                    & "repeats until their total growth is reached." + vbCrLf _
                    & "Growths may range from 0% to 255%.")
        GrowthsVarianceTooltip.SetToolTip(GrowthVarianceControl, "Before redistributing growths, a random amount from 0" + vbCrLf _
                                    & "and the amount entered (in increments of 5%) is either" + vbCrLf _
                                    & "added to or subtracted from a character's total growths." + vbCrLf _
                                    & "Maximum varaince is 100.")
        MinimumGrowthTooltip.SetToolTip(MinimumGrowthToggle, "Before redistributing growths, 5% is assigned to each" + vbCrLf _
                                        & "growth area and subtracted from the total. This ensures" + vbCrLf _
                                        & "the lowest growth a stat can have is 5%.")
        WeightedHPTooltip.SetToolTip(WeightedHPGrowthsToggle, "While redistributing growths, if the randomized stat is HP," + vbCrLf _
                                     & "then it will gain an additional 10% on top of the randomized amount." + vbCrLf _
                                     & "This makes it more likely HP will be the higher growth.")

        BasesTooltip.SetToolTip(RandomizeBasesToggle, "Applies random bases to all characters (playable and bosses)." + vbCrLf _
                                & "A character's original bases are added together and then" + vbCrLf _
                                & "redistributed across all stats to generate their new bases." + vbCrLf _
                                & "Redistribution is done by first randomizing a stat." + vbCrLf _
                                & "If the stat is HP, a random number between 1 and 5 is added to their HP base." + vbCrLf _
                                & "On STR/MAG/SPD/DEF, a random number between 1 and 2 is added to the stat base." + vbCrLf _
                                & "On SKL/RES, a random number between 1 and 3 is added to the stat base." + vbCrLf _
                                & "On LCK, a random number beween 1 and 4 is added to the stat base.")
        BaseVarianceTooltip.SetToolTip(BaseVarianceControl, "Before redistributing bases, a random amount from 0" + vbCrLf _
                                       & "and the amount entered is either added or subtracted from the" + vbCrLf _
                                       & "character's total growth. Maximum variance is 10.")

        CONTooltip.SetToolTip(RandomizeCONToggle, "Generates a random number between -5 and +5 and applies" + vbCrLf _
                              & "it to a character's original CON adjustment. This applies to" + vbCrLf _
                              & "both playable characters and bosses.")
        MinimumCONTooltip.SetToolTip(MinimumCONControl, "Enforces a minimum value for the CON stat for all characters." + vbCrLf _
                                     & "The CON value specified should be the final CON after applying adjustments." + vbCrLf _
                                     & "Any unit with a CON less than this value will have it brought up to this value.")

        MOVTooltip.SetToolTip(RandomizeMOVToggle, "Generates a random number between the specified minimum MOV" + vbCrLf _
                              & "and specified maximum MOV for all classes. Note that this applies on a class" + vbCrLf _
                              & "as opposed to a single character. The minimum MOV is 1 and the maximum MOV is 9.")
        MinimumMOVTooltip.SetToolTip(MinimumMOVControl, "Sets the minimum MOV allowed for any class." + vbCrLf _
                                     & "This value must be between 1 and the maximum MOV.")
        MaximumMOVTooltip.SetToolTip(MaximumMOVControl, "Sets the maximum MOV allowed for any class." + vbCrLf _
                                     & "This value must be between the minimum MOV and 9.")

        RandomAffinityTooltip.SetToolTip(RandomizeAffinityToggle, "Sets a random affinity for every character (playable" + vbCrLf _
                                         & "and bosses). Most enemies don't get support bonuses, so it really only" + vbCrLf _
                                         & "affects playable characters.")
        RandomAffinityBonusTooltip.SetToolTip(RandomizeAffinityBonusToggle, "Randomizes the bonuses given for support pairs. The bonuses available" + vbCrLf _
                                              & "Attack, Defense, Accuracy, Avoid, Critical, And Critical Evade." + vbCrLf _
                                              & "4 of the above are selected for each affinity and assigned a bonus value.")
        AffinityBonusAmountTooltip.SetToolTip(AffinityBonusAmountControl, "Determines the variance for bonus amounts. Values for" + vbCrLf _
                                              & "ATK/DEF vary from 1 to the number shown. Values for HIT/AVD/CRT/EVD vary from" + vbCrLf _
                                              & "5 to the number shown, in increments of 5 (i.e. 5, 10, 15, 20, etc.)." + vbCrLf _
                                              & "All values shown are bonuses given to a pair with the same affinity (e.g. Fire x Fire)" + vbCrLf _
                                              & "so the actual bonuses for any single affinity is halved and then added to" + vbCrLf _
                                              & "the other affinity (also halved) if pairing different affinities.")

        RandomItemsTooltip.SetToolTip(RandomizeItemsToggle, "Iterates through all weapons and applies a random adjustment to their" + vbCrLf _
                                      & "relevant stats. Only weapons are affected by this. Items are ignored.")
        MightVarianceTooltip.SetToolTip(MightVarianceControl, "Sets the maximum allowed variance on the MT stat of all weapons." + vbCrLf _
                                        & "A random number up to the number here is either added to or subtracted from a weapon's base MT." + vbCrLf _
                                        & "The maximum allowed variance is 10 (i.e. +/- 10).")
        MinimumMightTooltip.SetToolTip(MinimumMightControl, "Sets the minimum allowed MT on any weapon. Weapons with less MT" + vbCrLf _
                                       & "than the amount specified here will have their MT increased to this amount." + vbCrLf _
                                       & "This value must be between 0 and 10.")
        HitVarianceTooltip.SetToolTip(HitVarianceControl, "Sets the maximum allowed variance on the HIT stat of all weapons." + vbCrLf _
                                      & "A random number up to the number here (in increments of 5) is either added to or subtracted from" + vbCrLf _
                                      & "a weapon's base HIT. The maximum allowed variance is 100 (i.e. +/- 100).")
        MinimumHitTooltip.SetToolTip(MinimumHitControl, "Sets the minimum allowed HIT on any weapon. Weapons with less HIT" + vbCrLf _
                                     & "than the amount specified here will have their HIT increased to this amount." + vbCrLf _
                                     & "This value must be between 0 and 100.")
        CriticalVarianceTooltip.SetToolTip(CriticalVarianceControl, "Sets the maximum allowed variance on the CRIT stat of all weapons." + vbCrLf _
                                           & "A random number up to the number here (in increments of 5) is either added to or subtracted from" + vbCrLf _
                                           & "a weapon's base CRIT. The maximum allowed variance is 50 (i.e. +/- 50).")
        MinimumCriticalTooltip.SetToolTip(MinimumCriticalControl, "Sets the minimum allowed CRIT on any weapon. Weapons with less CRIT" + vbCrLf _
                                          & "than the amount specified here will have their CRIT increased to this amount." + vbCrLf _
                                          & "This value must be between 0 and 30.")
        WeightVarianceTooltip.SetToolTip(WeightVarianceControl, "Sets the maximum allowed variance on the WT stat of all weapons." + vbCrLf _
                                         & "A random number up to the number here is added to or subtracted from" + vbCrLf _
                                         & "a weapon's base WT. The maximum allowed variance is 10 (i.e. +/- 10).")
        MinimumWeightTooltip.SetToolTip(MinimumWeightControl, "Sets the minimum allowed WT on any weapon. Weapons with less WT" + vbCrLf _
                                        & "than the amount specified here will have their WT increased to this amount." + vbCrLf _
                                        & "This value must be between 1 and the maximum weight specified.")
        MaximumWeightTooltip.SetToolTip(MaximumWeightControl, "Sets the maximum allowed WT on any weapon. Weapons with more WT" + vbCrLf _
                                        & "than the amount specified here will have their WT reduced to this amount." + vbCrLf _
                                        & "This value must be between the minimum weight specified and 50.")
        DurabilityVarianceTooltip.SetToolTip(DurabilityVarianceControl, "Sets the maximum allowed variance on the durability of all weapons." + vbCrLf _
                                             & "A random number up to the number here is either added to or subtracted from" + vbCrLf _
                                             & "a weapon's durability. The maximum allowed variance is 49.")
        MinimumDurabilityTooltip.SetToolTip(MinimumDurabilityControl, "Sets the minimum allowed durability on any weapon. Weapons with" + vbCrLf _
                                            & "less durability than the amount specified here will have their durability increased" + vbCrLf _
                                            & "to this amount. This value must be between 1 and 50.")
        AllowRandomTraitsTooltip.SetToolTip(AllowRandomTraitsToggle, "While randomizing weapons, allow the random assignment of weapon traits" + vbCrLf _
                                            & "to weapons. This includes 1 of the following: Reverses Weapon Triangle, Unbreakable, Brave," + vbCrLf _
                                            & "Magic Damage (physical weapons only), Uncounterable, A random effectiveness, A Random Stat Bonus, Poison On Hit," + vbCrLf _
                                            & "Halves HP, or Devil Reversal. For FE7 and FE8, Negates Defenses is also a possibility." + vbCrLf _
                                            & "Weapons already with an effect retain their original effect in addition to a new random trait.")

        RandomClassesTooltip.SetToolTip(RandomizeClassesToggle, "Assigns a random class to characters in the chapter data." + vbCrLf _
                                        & "By default, this randomizes all non-lord, non-thief playable characters." + vbCrLf _
                                        & "All units will have their inventories reset to allow them to be effective upon joining." + vbCrLf _
                                        & "Weapon ranks will also be reset to correspond to their respective classes." + vbCrLf _
                                        & "Only classes that can promote (or already promoted classes) will show up by default.")
        RandomizeLordsTooltip.SetToolTip(IncludeLordsToggle, "Also randomize lord characters to different classes. Also adds lords to" + vbCrLf _
                                         & "the pool of classes available for other units.")
        RandomizeThievesTooltip.SetToolTip(IncludeThievesToggle, "Also randomize thief characters to different classes. Also adds thieves to" + vbCrLf _
                                           & "the pool of classes available for other units.")
        RandomizeBossesTooltip.SetToolTip(IncludeBossesToggle, "Also randomize boss characters to different classes. This does not modify classes" + vbCrLf _
                                          & "of boss characters with unique classes (most final bosses).")
        UniqueClassesTooltip.SetToolTip(AllowUniqueClassesToggle, "Allow unique classes from each game into the random pool of classes available." + vbCrLf _
                                        & "Additionally, this allows classes that do not promote (such as Soldier) into the pool as well and they" + vbCrLf _
                                        & "will continue to be unpromotable. For FE8, this adds monster classes to the pool.")

        EnemyBuffTooltip.SetToolTip(IncreaseEnemyGrowthsToggle, "Increases the growths of all classes by a constant or random amount." + vbCrLf _
                                    & "Playable characters override this growth so this does not affect them.")
        BuffAmountTooltip.SetToolTip(EnemyBuffControl, "Sets the amount to buff all class growths by. The options below" + vbCrLf _
                                     & "dictate how to use this value." + vbCrLf _
                                     & """Up To Amount"" generates a random value beween 0 and the value specified to be added to growths." + vbCrLf _
                                     & """Exactly Amount"" adds the listed value to all stat areas for all classes." + vbCrLf _
                                     & """At Least Amount"" adds a random value between the listed amount and 255 to the growths (capping at 255 growth).")
        BossBuffTooltip.SetToolTip(BuffBossesToggle, "By default, bosses also override growths and will not autolevel to match their" + vbCrLf _
                                   & "subordinates. Enabling this adjusts this by applying a small amount to bases to correct for that.")

        RecruitmentTooltip.SetToolTip(RecruitmentBox, "Modifies the recruitment order for the game." + vbCrLf _
                                      & """Normal"" is the default order." + vbCrLf _
                                      & """Reversed"" mirrors the recruitment order, making the last few characters join first and vice versa." + vbCrLf _
                                      & """Randomized"" takes all of the characters and drops them into random recruitment slots." + vbCrLf _
                                      & "Note that dialogue referring to characters does not change.")
    End Sub
End Class