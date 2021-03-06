// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using FluentAssertions;
using NuGet.Common;
using NuGet.Test.Utility;
using Xunit;

namespace NuGet.Configuration.Test
{
    public class AddItemTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddItem_Constructor_WithEmptyOrNullKey_Throws(string key)
        {
            var ex = Record.Exception(() => new AddItem(key, "value"));
            ex.Should().NotBeNull();
            ex.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void AddItem_WithoutRequiredAttributes_Throws()
        {
            // Arrange
            var config = @"
<configuration>
    <SectionName>
        <add Key='key2' Value='value2' />,
    </SectionName>
</configuration>";
            var nugetConfigPath = "NuGet.Config";
            using (var mockBaseDirectory = TestDirectory.Create())
            {
                ConfigurationFileTestUtility.CreateConfigurationFile(nugetConfigPath, mockBaseDirectory, config);

                // Act and Assert
                var ex = Record.Exception(() => new SettingsFile(mockBaseDirectory));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<NuGetConfigurationException>();
                ex.Message.Should().Be(string.Format("Unable to parse config file because: Missing required attribute 'key' in element 'add'. Path: '{0}'.", Path.Combine(mockBaseDirectory, nugetConfigPath)));
            }
        }

        [Fact]
        public void SourceItem_CaseInsensitive_ParsedSuccessfully()
        {
            // Arrange
            var config = @"
<configuration>
    <section>
        <AdD key='key' value='val' />
    </section>
</configuration>";

            var expectedValue = new AddItem("key", "val");

            var nugetConfigPath = "NuGet.Config";
            using (var mockBaseDirectory = TestDirectory.Create())
            {
                ConfigurationFileTestUtility.CreateConfigurationFile(nugetConfigPath, mockBaseDirectory, config);

                // Act and Assert
                var settingsFile = new SettingsFile(mockBaseDirectory);
                settingsFile.Should().NotBeNull();

                var section = settingsFile.GetSection("section");
                section.Should().NotBeNull();

                var children = section.Items.ToList();

                children.Should().NotBeEmpty();
                children.Count.Should().Be(1);

                children[0].DeepEquals(expectedValue).Should().BeTrue();
            }
        }

        [Fact]
        public void AddItem_SingleTag_WithOnlyKeyAndValue_ParsedSuccessfully()
        {
            // Arrange
            var nugetConfigPath = "NuGet.Config";
            var config = @"
<configuration>
    <Section>
        <add key='key1' value='value1' />
    </Section>
</configuration>";

            var expectedSetting = new AddItem("key1", "value1");

            using (var mockBaseDirectory = TestDirectory.Create())
            {
                ConfigurationFileTestUtility.CreateConfigurationFile(nugetConfigPath, mockBaseDirectory, config);
                var settingsFile = new SettingsFile(mockBaseDirectory);

                // Act
                var section = settingsFile.GetSection("Section");
                section.Should().NotBeNull();

                var element = section.Items.FirstOrDefault();
                element.Should().NotBeNull();

                // Assert
                element.DeepEquals(expectedSetting).Should().BeTrue();
            }
        }

        [Fact]
        public void AddItem_MultiTag_WithOnlyKeyAndValue_ParsedSuccessfully()
        {
            // Arrange
            var nugetConfigPath = "NuGet.Config";
            var config = @"
<configuration>
    <Section>
        <add key='key1' value='value1'></add>
    </Section>
</configuration>";

            var expectedSetting = new AddItem("key1", "value1");

            using (var mockBaseDirectory = TestDirectory.Create())
            {
                ConfigurationFileTestUtility.CreateConfigurationFile(nugetConfigPath, mockBaseDirectory, config);
                var settingsFile = new SettingsFile(mockBaseDirectory);

                // Act
                var section = settingsFile.GetSection("Section");
                section.Should().NotBeNull();

                var element = section.Items.FirstOrDefault();
                element.Should().NotBeNull();

                // Assert
                element.DeepEquals(expectedSetting).Should().BeTrue();
            }
        }

        [Fact]
        public void AddItem_WithAdditionalMetada_ParsedSuccessfully()
        {
            // Arrange
            var nugetConfigPath = "NuGet.Config";
            var config = @"
<configuration>
    <Section>
        <add key='key1' value='value1' meta1='data1' meta2='data2'/>
    </Section>
</configuration>";

            var expectedSetting = new AddItem("key1", "value1",
                new ReadOnlyDictionary<string, string>(new Dictionary<string, string>{
                    { "meta1", "data1" },
                    { "meta2", "data2" }
                }));

            using (var mockBaseDirectory = TestDirectory.Create())
            {
                ConfigurationFileTestUtility.CreateConfigurationFile(nugetConfigPath, mockBaseDirectory, config);
                var settingsFile = new SettingsFile(mockBaseDirectory);

                // Act
                var section = settingsFile.GetSection("Section");
                section.Should().NotBeNull();

                var value = section.Items.FirstOrDefault();
                value.Should().NotBeNull();

                // Assert
                value.DeepEquals(expectedSetting).Should().BeTrue();
            }
        }

        [Fact]
        public void AddItem_Parsing_ElementWithChildren_Throws()
        {
            // Arrange
            var nugetConfigPath = "NuGet.Config";
            var config = @"
<configuration>
    <Section>
        <add key='key1' value='value1'>
            <add key='key2' value='value2' />
        </add>
    </Section>
</configuration>";

            using (var mockBaseDirectory = TestDirectory.Create())
            {
                ConfigurationFileTestUtility.CreateConfigurationFile(nugetConfigPath, mockBaseDirectory, config);

                // Act and Assert
                var ex = Record.Exception(() => new SettingsFile(mockBaseDirectory));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<NuGetConfigurationException>();
                ex.Message.Should().Be(string.Format("Error parsing NuGet.Config. Element '{0}' cannot have descendant elements. Path: '{1}'.", "add", Path.Combine(mockBaseDirectory, nugetConfigPath)));
            }
        }

        [Fact]
        public void AddItem_UpdatingAttribute_WithAddOrUpdate_SuccessfullyUpdatesConfigFile()
        {
            // Arrange
            var nugetConfigPath = "NuGet.Config";
            var config = @"
<configuration>
    <Section>
        <add key='key1' value='value1' />
    </Section>
</configuration>";

            using (var mockBaseDirectory = TestDirectory.Create())
            {
                ConfigurationFileTestUtility.CreateConfigurationFile(nugetConfigPath, mockBaseDirectory, config);
                var configFileHash = ConfigurationFileTestUtility.GetFileHash(Path.Combine(mockBaseDirectory, nugetConfigPath));

                // Act and Assert
                var settingsFile = new SettingsFile(mockBaseDirectory);

                // Act
                var section = settingsFile.GetSection("Section");
                section.Should().NotBeNull();

                var element = section.Items.FirstOrDefault() as AddItem;
                element.Should().NotBeNull();

                element.Value = "newValue";
                element.Value.Should().Be("newValue");

                var section2 = settingsFile.GetSection("Section");
                section2.Should().NotBeNull();

                var element2 = section2.Items.FirstOrDefault() as AddItem;
                element2.Should().NotBeNull();
                element2.Value.Should().Be("value1");

                settingsFile.AddOrUpdate("Section", element);
                settingsFile.SaveToDisk();

                var section3 = settingsFile.GetSection("Section");
                section3.Should().NotBeNull();

                var element3 = section.Items.FirstOrDefault() as AddItem;
                element3.Should().NotBeNull();
                element3.Value.Should().Be("newValue");

                var updatedFileHash = ConfigurationFileTestUtility.GetFileHash(Path.Combine(mockBaseDirectory, nugetConfigPath));
                updatedFileHash.Should().NotBeEquivalentTo(configFileHash);
            }
        }

        [Fact]
        public void AddItem_GetValueAsPath_ResolvesPathsCorrectly()
        {
            // Arrange
            var nugetConfigPath = "NuGet.Config";
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <SectionName>
        <!-- values that are relative paths -->
        <add key=""key1"" value=""..\value1"" />
        <add key=""key2"" value=""a\b\c"" />
        <add key=""key3"" value="".\a\b\c"" />

        <!-- values that are not relative paths -->
        <add key=""key4"" value=""c:\value2"" />
        <add key=""key5"" value=""http://value3"" />
        <add key=""key6"" value=""\\a\b\c"" />
        <add key=""key7"" value=""\a\b\c"" />
    </SectionName>
</configuration>";

            if (!RuntimeEnvironmentHelper.IsWindows)
            {
                config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <SectionName>
        <!-- values that are relative paths -->
        <add key=""key1"" value=""../value1"" />
        <add key=""key2"" value=""a/b/c"" />
        <add key=""key3"" value=""./a/b/c"" />

        <!-- values that are not relative paths -->
        <add key=""key5"" value=""http://value3"" />
        <add key=""key7"" value=""/a/b/c"" />
    </SectionName>
</configuration>";
            }

            using (var mockBaseDirectory = TestDirectory.Create())
            {
                ConfigurationFileTestUtility.CreateConfigurationFile(nugetConfigPath, mockBaseDirectory, config);
                var settings = new Settings(mockBaseDirectory);

                // Act
                var section = settings.GetSection("SectionName");

                // Assert
                section.Should().NotBeNull();
                section.Items.Should().NotBeEmpty();
                section.Items.Should().AllBeOfType<AddItem>();

                if (RuntimeEnvironmentHelper.IsWindows)
                {
                    section.Items.Count.Should().Be(7);

                    var item = section.GetFirstItemWithAttribute<AddItem>("key", "key1");
                    item.Should().NotBeNull();
                    item.GetValueAsPath().Should().Be(Path.Combine(mockBaseDirectory, @"..\value1"));
                    item = section.GetFirstItemWithAttribute<AddItem>("key", "key2");
                    item.Should().NotBeNull();
                    item.GetValueAsPath().Should().Be(Path.Combine(mockBaseDirectory, @"a\b\c"));
                    item = section.GetFirstItemWithAttribute<AddItem>("key", "key3");
                    item.Should().NotBeNull();
                    item.GetValueAsPath().Should().Be(Path.Combine(mockBaseDirectory, @".\a\b\c"));
                    item = section.GetFirstItemWithAttribute<AddItem>("key", "key4");
                    item.Should().NotBeNull();
                    item.GetValueAsPath().Should().Be(@"c:\value2");
                    item = section.GetFirstItemWithAttribute<AddItem>("key", "key5");
                    item.Should().NotBeNull();
                    item.GetValueAsPath().Should().Be(@"http://value3");
                    item = section.GetFirstItemWithAttribute<AddItem>("key", "key6");
                    item.Should().NotBeNull();
                    item.GetValueAsPath().Should().Be(@"\\a\b\c");
                    item = section.GetFirstItemWithAttribute<AddItem>("key", "key7");
                    item.Should().NotBeNull();
                    item.GetValueAsPath().Should().Be(Path.Combine(Path.GetPathRoot(mockBaseDirectory), @"a\b\c"));
                }
                else
                {
                    section.Items.Count.Should().Be(5);

                    var item = section.GetFirstItemWithAttribute<AddItem>("key", "key1");
                    item.Should().NotBeNull();
                    item.GetValueAsPath().Should().Be(Path.Combine(mockBaseDirectory, @"../value1"));
                    item = section.GetFirstItemWithAttribute<AddItem>("key", "key2");
                    item.Should().NotBeNull();
                    item.GetValueAsPath().Should().Be(Path.Combine(mockBaseDirectory, @"a/b/c"));
                    item = section.GetFirstItemWithAttribute<AddItem>("key", "key3");
                    item.Should().NotBeNull();
                    item.GetValueAsPath().Should().Be(Path.Combine(mockBaseDirectory, @"./a/b/c"));
                    item = section.GetFirstItemWithAttribute<AddItem>("key", "key5");
                    item.Should().NotBeNull();
                    item.GetValueAsPath().Should().Be(@"http://value3");
                    item = section.GetFirstItemWithAttribute<AddItem>("key", "key7");
                    item.Should().NotBeNull();
                    item.GetValueAsPath().Should().Be(@"/a/b/c");
                }
            }
        }
    }
}
