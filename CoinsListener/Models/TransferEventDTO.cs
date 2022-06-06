using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

using System.Numerics;

namespace CoinsListener.Models
{

    public class BaseTransferEventDTO : IEventDTO
    {
        [Parameter("address", "_from", 1, true)]
        public string From { get; set; }

        [Parameter("address", "_to", 2, true)] public string To { get; set; }

        [Parameter("uint256", "_value", 3, false)]
        public BigInteger Value { get; set; }
    }


    // Define event we want to look for
    // Note: in a visual studio project outside of this playground, you have the option to install nuget package
    // Nethereum.StandardTokenEIP20 and add a using Nethereum.StandardTokenEIP20 to provide class TransferEventDTO,
    // instead of defining it here.
    [Event("Transfer")]
    public partial class TransferEventDTO : BaseTransferEventDTO
    {

    }

    [Event("Set Fixed Fee")]
    public class SetFixedFeeDTO : BaseTransferEventDTO
    {

    }

    [Event("Set Percent Fee")]
    public class SetPercentFeeDTO : BaseTransferEventDTO
    {

    }

    //[Event("0x60806040")]
}
