using NetDiscordRpc.Core.Converters;

namespace NetDiscordRpc.RPC.Payload
{
    public enum Commands
    {
	    [EnumValue("DISPATCH")]
		Dispatch,
                
		[EnumValue("SET_ACTIVITY")]
		SetActivity,
		
		[EnumValue("SUBSCRIBE")]
		Subscribe,
		
		[EnumValue("UNSUBSCRIBE")]
		Unsubscribe,
		
		[EnumValue("SEND_ACTIVITY_JOIN_INVITE")]
		SendActivityJoinInvite,
		
		[EnumValue("CLOSE_ACTIVITY_JOIN_REQUEST")]
		CloseActivityJoinRequest
    }
}