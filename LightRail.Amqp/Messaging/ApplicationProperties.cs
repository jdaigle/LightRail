using System;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Messaging
{
    /// <remarks>
    /// The application-properties section is a part of the bare message used for structured application data. Intermediaries
    /// can use the data within this structure for the purposes of filtering or routing.
    /// 
    /// <type name="application-properties" class="restricted" source="map" provides="section">
    ///     <descriptor name = "amqp:application-properties:map" code="0x00000000:0x00000074"/>
    /// </type>
    /// 
    /// The keys of this map are restricted to be of type string (which excludes the possibility of a null key) and the
    /// values are restricted to be of simple types only, that is, excluding map, list, and array types.
    /// </remarks>
    public sealed class ApplicationProperties : DescribedList // TODO: this should be a map, not a list
    {
        public ApplicationProperties() : base(MessagingDescriptors.ApplicationProperties) { }
    }
}