﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Event filter
    /// </summary>
    [DataContract]
    public class EventFilterApiModel {

        /// <summary>
        /// Select statements
        /// </summary>
        [DataMember(Name = "selectClauses", Order = 0)]
        public List<SimpleAttributeOperandApiModel> SelectClauses { get; set; }

        /// <summary>
        /// Where clause
        /// </summary>
        [DataMember(Name = "whereClause", Order = 1)]
        public ContentFilterApiModel WhereClause { get; set; }

        /// <summary>
        /// Settings for pending alarms
        /// </summary>
        [DataMember(Name = "pendingAlarms", Order = 2,
            EmitDefaultValue = false)]
        public PendingAlarmsOptionsApiModel PendingAlarms { get; set; }

        /// <summary>
        /// Simple event Type id
        /// </summary>
        [DataMember(Name = "typeDefinitionId", Order = 3,
            EmitDefaultValue = false)]
        public string TypeDefinitionId { get; set; }
    }
}