﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.Standalone {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using TestExtensions;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;
    using static System.TimeSpan;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeTraitValue)]
    public class C_PublishConditionsTestTheory : DynamicAciTestBase, IClassFixture<IIoTStandaloneTestContext> {
        private static readonly TimeSpan Precision = FromMilliseconds(500);

        public C_PublishConditionsTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output)
            : base(context, output) {
        }

        [Fact, PriorityOrder(11)]
        public async void TestACI_VerifyDataAvailableAtIoTHub_Expect_NumberOfEvents_GreaterThan_Zero() {

            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --dalm=files/sc001.json --pn=50000"},
                _timeoutToken,
                "opc-plc-files/sc001.json");

            var messages = _consumer.ReadMessagesFromWriterIdAsync<ConditionTypePayload>(_writerId, _timeoutToken);

            // Act
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter());
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);

            const int nMessages = 6;
            var payloads = await messages
                .Select(e => e.Messages["i=2253"])
                .Skip(nMessages) // First batch of alarms are from a ConditionRefresh, therefore not in order
                .SkipWhile(c => !c.Message.Contains("LAST EVENT IN LOOP"))
                .Skip(1)
                .Take(nMessages)
                .ToListAsync(_timeoutToken);

            // Assert

            var i = -1;
            var doorOpen = new ConditionTypePayload {
                ConditionName = "VendingMachine1_DoorOpen",
                EnabledState = "Enabled",
                EnabledStateEffectiveDisplayName = "Active | Unacknowledged",
                EnabledStateId = true,
                EventType = "i=10751",
                LastSeverity = 500,
                Message = "Door Open",
                Retain = true,
                Severity = 900,
                SourceName = "VendingMachine1",
                SourceNode = "http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#s=VendingMachine1",
            };
            VerifyPayload(payloads, ++i, null, doorOpen);

            VerifyPayload(payloads,
                ++i,
                FromSeconds(5),
                new ConditionTypePayload {
                    ConditionName = "VendingMachine2_LightOff",
                    EnabledState = "Enabled",
                    EnabledStateEffectiveDisplayName = "Active | Unacknowledged",
                    EnabledStateId = true,
                    EventType = "i=10637",
                    LastSeverity = 500,
                    Message = "Light Off in machine",
                    Retain = true,
                    Severity = 500,
                    SourceName = "VendingMachine2",
                    SourceNode = "http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#s=VendingMachine2",
                });

            VerifyPayload(payloads,
                ++i,
                Zero,
                new ConditionTypePayload {
                    ConditionName = "VendingMachine1_AD_Lamp_Off",
                    EnabledState = "Enabled",
                    EnabledStateEffectiveDisplayName = "Enabled",
                    EnabledStateId = true,
                    EventType = "i=2782",
                    LastSeverity = 500,
                    Message = "AD Lamp Off",
                    Retain = true,
                    Severity = 500,
                    SourceName = "VendingMachine1",
                    SourceNode = "http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#s=VendingMachine1",
                });

            VerifyPayload(payloads,
                ++i,
                FromSeconds(5),
                new ConditionTypePayload {
                    ConditionName = "VendingMachine1_DoorOpen",
                    EnabledState = "Enabled",
                    EnabledStateEffectiveDisplayName = "Inactive | Unacknowledged",
                    EnabledStateId = true,
                    EventType = "i=10751",
                    LastSeverity = 900,
                    Message = "Door Closed",
                    Retain = false,
                    Severity = 500,
                    SourceName = "VendingMachine1",
                    SourceNode = "http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#s=VendingMachine1",
                });

            VerifyPayload(payloads,
                ++i,
                FromSeconds(4),
                new ConditionTypePayload {
                    ConditionName = "VendingMachine1_TemperatureHigh",
                    EnabledState = "Enabled",
                    EnabledStateEffectiveDisplayName = "Active | Unacknowledged",
                    EnabledStateId = true,
                    EventType = "i=2955",
                    LastSeverity = 900,
                    Message = "Temperature is HIGH (LAST EVENT IN LOOP)",
                    Retain = true,
                    Severity = 900,
                    SourceName = "VendingMachine1",
                    SourceNode = "http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#s=VendingMachine1",
                });

            VerifyPayload(payloads, ++i, Zero, doorOpen); // cycling back to first message
        }

        private static void VerifyPayload(IReadOnlyList<ConditionTypePayload> payloads, int i, TimeSpan? expectedDelay, ConditionTypePayload expectedPayload) {
            var p = payloads[i];
            p.Should().BeEquivalentTo(expectedPayload,
                opt => opt // ignore non-constant properties
                    .Excluding(c => c.EventId)
                    .Excluding(c => c.ConditionId)
                    .Excluding(m => m.Type == typeof(DateTime) || m.Type == typeof(DateTime?)));

            p.ConditionId.Should().StartWith("http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#i=");

            p.CommentSourceTimestamp.Should().BeCloseTo(p.ReceiveTime.Value, Precision);
            p.EnabledStateEffectiveTransitionTime.Should().BeCloseTo(p.ReceiveTime.Value, Precision);
            p.EnabledStateTransitionTime.Should().BeCloseTo(p.ReceiveTime.Value, Precision);
            p.LastSeveritySourceTimestamp.Should().BeCloseTo(p.ReceiveTime.Value, Precision);

            if (expectedDelay != null) {
                i.Should().BeGreaterThan(0);
                var transitionTime = (p.EnabledStateEffectiveTransitionTime - payloads[i - 1].EnabledStateEffectiveTransitionTime);
               // TODO there is no difference in the transition time...
               // transitionTime.Should().BeCloseTo(expectedDelay.Value, Precision);
            }
        }
    }
}