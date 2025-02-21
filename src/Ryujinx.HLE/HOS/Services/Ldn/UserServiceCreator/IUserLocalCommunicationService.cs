using LibHac.Ns;
using Ryujinx.Common;
using Ryujinx.Common.Configuration.Multiplayer;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Memory;
using Ryujinx.Common.Utilities;
using Ryujinx.Cpu;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnMitm;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
using Ryujinx.Horizon.Common;
using Ryujinx.Memory;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    class IUserLocalCommunicationService : IpcService, IDisposable
    {
        public INetworkClient NetworkClient { get; private set; }

        private const int NifmRequestID = 90;
        private const string DefaultIPAddress = "127.0.0.1";
        private const string DefaultSubnetMask = "255.255.255.0";
        private const bool IsDevelopment = false;

        private readonly KEvent _stateChangeEvent;
        private int _stateChangeEventHandle;

        private NetworkState _state;
        private DisconnectReason _disconnectReason;
        private ResultCode _nifmResultCode;

        private AccessPoint _accessPoint;
        private Station _station;

        public IUserLocalCommunicationService(ServiceCtx context)
        {
            _stateChangeEvent = new KEvent(context.Device.System.KernelContext);
            _state = NetworkState.None;
            _disconnectReason = DisconnectReason.None;
        }

        private ushort CheckDevelopmentChannel(ushort channel)
        {
            return (ushort)(!IsDevelopment ? 0 : channel);
        }

        private SecurityMode CheckDevelopmentSecurityMode(SecurityMode securityMode)
        {
            return !IsDevelopment ? SecurityMode.Retail : securityMode;
        }

        private bool CheckLocalCommunicationIdPermission(ServiceCtx context, ulong localCommunicationIdChecked)
        {
            // TODO: Call nn::arp::GetApplicationControlProperty here when implemented.
            ApplicationControlProperty controlProperty = context.Device.Processes.ActiveApplication.ApplicationControlProperties;

            foreach (ulong localCommunicationId in controlProperty.LocalCommunicationId.ItemsRo)
            {
                if (localCommunicationId == localCommunicationIdChecked)
                {
                    return true;
                }
            }

            return false;
        }

        [CommandCmif(0)]
        // GetState() -> s32 state
        public ResultCode GetState(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                context.ResponseData.Write((int)NetworkState.Error);

                return ResultCode.Success;
            }

            // NOTE: Returns ResultCode.InvalidArgument if _state is null, doesn't occur in our case.
            context.ResponseData.Write((int)_state);

            return ResultCode.Success;
        }

        public void SetState()
        {
            _stateChangeEvent.WritableEvent.Signal();
        }

        public void SetState(NetworkState state)
        {
            _state = state;

            SetState();
        }

        [CommandCmif(1)]
        // GetNetworkInfo() -> buffer<network_info<0x480>, 0x1a>
        public ResultCode GetNetworkInfo(ServiceCtx context)
        {
            ulong bufferPosition = context.Request.RecvListBuff[0].Position;

            MemoryHelper.FillWithZeros(context.Memory, bufferPosition, 0x480);

            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            ResultCode resultCode = GetNetworkInfoImpl(out NetworkInfo networkInfo);
            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            ulong infoSize = MemoryHelper.Write(context.Memory, bufferPosition, networkInfo);

            context.Response.PtrBuff[0] = context.Response.PtrBuff[0].WithSize(infoSize);

            return ResultCode.Success;
        }

        private ResultCode GetNetworkInfoImpl(out NetworkInfo networkInfo)
        {
            if (_state == NetworkState.StationConnected)
            {
                networkInfo = _station.NetworkInfo;
            }
            else if (_state == NetworkState.AccessPointCreated)
            {
                networkInfo = _accessPoint.NetworkInfo;
            }
            else
            {
                networkInfo = new NetworkInfo();

                return ResultCode.InvalidState;
            }

            return ResultCode.Success;
        }

        private NodeLatestUpdate[] GetNodeLatestUpdateImpl(int count)
        {
            if (_state == NetworkState.StationConnected)
            {
                return _station.LatestUpdates.ConsumeLatestUpdate(count);
            }
            else if (_state == NetworkState.AccessPointCreated)
            {
                return _accessPoint.LatestUpdates.ConsumeLatestUpdate(count);
            }
            else
            {
                return [];
            }
        }

        [CommandCmif(2)]
        // GetIpv4Address() -> (u32 ip_address, u32 subnet_mask)
        public ResultCode GetIpv4Address(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            // NOTE: Return ResultCode.InvalidArgument if ip_address and subnet_mask are null, doesn't occur in our case.

            if (_state == NetworkState.AccessPointCreated || _state == NetworkState.StationConnected)
            {
                ProxyConfig config = _state switch
                {
                    NetworkState.AccessPointCreated => _accessPoint.Config,
                    NetworkState.StationConnected => _station.Config,

                    _ => default
                };

                if (config.ProxyIp == 0)
                {
                    (_, UnicastIPAddressInformation unicastAddress) = NetworkHelpers.GetLocalInterface(context.Device.Configuration.MultiplayerLanInterfaceId);

                    if (unicastAddress == null)
                    {
                        context.ResponseData.Write(NetworkHelpers.ConvertIpv4Address(DefaultIPAddress));
                        context.ResponseData.Write(NetworkHelpers.ConvertIpv4Address(DefaultSubnetMask));
                    }
                    else
                    {
                        Logger.Info?.Print(LogClass.ServiceLdn, $"Console's LDN IP is \"{unicastAddress.Address}\".");

                        context.ResponseData.Write(NetworkHelpers.ConvertIpv4Address(unicastAddress.Address));
                        context.ResponseData.Write(NetworkHelpers.ConvertIpv4Address(unicastAddress.IPv4Mask));
                    }
                }
                else
                {
                    Logger.Info?.Print(LogClass.ServiceLdn, $"LDN obtained proxy IP.");

                    context.ResponseData.Write(config.ProxyIp);
                    context.ResponseData.Write(config.ProxySubnetMask);
                }
            }
            else
            {
                return ResultCode.InvalidArgument;
            }

            return ResultCode.Success;
        }

        [CommandCmif(3)]
        // GetDisconnectReason() -> u16 disconnect_reason
        public ResultCode GetDisconnectReason(ServiceCtx context)
        {
            // NOTE: Returns ResultCode.InvalidArgument if _disconnectReason is null, doesn't occur in our case.

            context.ResponseData.Write((short)_disconnectReason);

            return ResultCode.Success;
        }

        public void SetDisconnectReason(DisconnectReason reason)
        {
            if (_state != NetworkState.Initialized)
            {
                _disconnectReason = reason;

                SetState(NetworkState.Initialized);
            }
        }

        [CommandCmif(4)]
        // GetSecurityParameter() -> bytes<0x20, 1> security_parameter
        public ResultCode GetSecurityParameter(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            ResultCode resultCode = GetNetworkInfoImpl(out NetworkInfo networkInfo);
            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            SecurityParameter securityParameter = new()
            {
                Data = new Array16<byte>(),
                SessionId = networkInfo.NetworkId.SessionId,
            };

            context.ResponseData.WriteStruct(securityParameter);

            return ResultCode.Success;
        }

        [CommandCmif(5)]
        // GetNetworkConfig() -> bytes<0x20, 8> network_config
        public ResultCode GetNetworkConfig(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            ResultCode resultCode = GetNetworkInfoImpl(out NetworkInfo networkInfo);
            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            NetworkConfig networkConfig = new()
            {
                IntentId = networkInfo.NetworkId.IntentId,
                Channel = networkInfo.Common.Channel,
                NodeCountMax = networkInfo.Ldn.NodeCountMax,
                LocalCommunicationVersion = networkInfo.Ldn.Nodes[0].LocalCommunicationVersion,
                Reserved2 = new Array10<byte>(),
            };

            context.ResponseData.WriteStruct(networkConfig);

            return ResultCode.Success;
        }

        [CommandCmif(100)]
        // AttachStateChangeEvent() -> handle<copy>
        public ResultCode AttachStateChangeEvent(ServiceCtx context)
        {
            if (_stateChangeEventHandle == 0 && context.Process.HandleTable.GenerateHandle(_stateChangeEvent.ReadableEvent, out _stateChangeEventHandle) != Result.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_stateChangeEventHandle);

            // Returns ResultCode.InvalidArgument if handle is null, doesn't occur in our case since we already throw an Exception.

            return ResultCode.Success;
        }

        [CommandCmif(101)]
        // GetNetworkInfoLatestUpdate() -> (buffer<network_info<0x480>, 0x1a>, buffer<node_latest_update, 0xa>)
        public ResultCode GetNetworkInfoLatestUpdate(ServiceCtx context)
        {
            ulong bufferPosition = context.Request.RecvListBuff[0].Position;

            MemoryHelper.FillWithZeros(context.Memory, bufferPosition, 0x480);

            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            ResultCode resultCode = GetNetworkInfoImpl(out NetworkInfo networkInfo);
            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            ulong outputPosition = context.Request.RecvListBuff[0].Position;
            ulong outputSize = context.Request.RecvListBuff[0].Size;

            ulong latestUpdateSize = (ulong)Marshal.SizeOf<NodeLatestUpdate>();
            int count = (int)(outputSize / latestUpdateSize);

            NodeLatestUpdate[] latestUpdate = GetNodeLatestUpdateImpl(count);

            MemoryHelper.FillWithZeros(context.Memory, outputPosition, (int)outputSize);

            foreach (NodeLatestUpdate node in latestUpdate)
            {
                MemoryHelper.Write(context.Memory, outputPosition, node);

                outputPosition += latestUpdateSize;
            }

            ulong infoSize = MemoryHelper.Write(context.Memory, bufferPosition, networkInfo);

            context.Response.PtrBuff[0] = context.Response.PtrBuff[0].WithSize(infoSize);

            return ResultCode.Success;
        }

        [CommandCmif(102)]
        // Scan(u16 channel, bytes<0x60, 8> scan_filter) -> (u16 count, buffer<network_info, 0x22>)
        public ResultCode Scan(ServiceCtx context)
        {
            return ScanImpl(context);
        }

        [CommandCmif(103)]
        // ScanPrivate(u16 channel, bytes<0x60, 8> scan_filter) -> (u16 count, buffer<network_info, 0x22>)
        public ResultCode ScanPrivate(ServiceCtx context)
        {
            return ScanImpl(context, true);
        }

        private ResultCode ScanImpl(ServiceCtx context, bool isPrivate = false)
        {
            ushort channel = (ushort)context.RequestData.ReadUInt64();
            ScanFilter scanFilter = context.RequestData.ReadStruct<ScanFilter>();

            (ulong bufferPosition, ulong bufferSize) = context.Request.GetBufferType0x22(0);

            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (!isPrivate)
            {
                channel = CheckDevelopmentChannel(channel);
            }

            ResultCode resultCode = ResultCode.InvalidArgument;

            if (bufferSize != 0)
            {
                if (bufferPosition != 0)
                {
                    ScanFilterFlag scanFilterFlag = scanFilter.Flag;

                    if (!scanFilterFlag.HasFlag(ScanFilterFlag.NetworkType) || scanFilter.NetworkType <= NetworkType.All)
                    {
                        if (scanFilterFlag.HasFlag(ScanFilterFlag.Ssid))
                        {
                            if (scanFilter.Ssid.Length <= 31)
                            {
                                return resultCode;
                            }
                        }

                        if (!scanFilterFlag.HasFlag(ScanFilterFlag.MacAddress))
                        {
                            if (scanFilterFlag > ScanFilterFlag.All)
                            {
                                return resultCode;
                            }

                            if (_state - 3 >= NetworkState.AccessPoint)
                            {
                                resultCode = ResultCode.InvalidState;
                            }
                            else
                            {
                                if (scanFilter.NetworkId.IntentId.LocalCommunicationId == -1 && NetworkClient.NeedsRealId)
                                {
                                    // TODO: Call nn::arp::GetApplicationControlProperty here when implemented.
                                    ApplicationControlProperty controlProperty = context.Device.Processes.ActiveApplication.ApplicationControlProperties;

                                    scanFilter.NetworkId.IntentId.LocalCommunicationId = (long)controlProperty.LocalCommunicationId[0];
                                }

                                resultCode = ScanInternal(context.Memory, channel, scanFilter, bufferPosition, bufferSize, out ulong counter);

                                context.ResponseData.Write(counter);
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
            }

            return resultCode;
        }

        private ResultCode ScanInternal(IVirtualMemoryManager memory, ushort channel, ScanFilter scanFilter, ulong bufferPosition, ulong bufferSize, out ulong counter)
        {
            ulong networkInfoSize = (ulong)Marshal.SizeOf<NetworkInfo>();
            ulong maxGames = bufferSize / networkInfoSize;

            MemoryHelper.FillWithZeros(memory, bufferPosition, (int)bufferSize);

            NetworkInfo[] availableGames = NetworkClient.Scan(channel, scanFilter);

            counter = 0;

            foreach (NetworkInfo networkInfo in availableGames)
            {
                MemoryHelper.Write(memory, bufferPosition + (networkInfoSize * counter), networkInfo);

                if (++counter >= maxGames)
                {
                    break;
                }
            }

            return ResultCode.Success;
        }

        [CommandCmif(104)] // 5.0.0+
        // SetWirelessControllerRestriction(u32 wireless_controller_restriction)
        public ResultCode SetWirelessControllerRestriction(ServiceCtx context)
        {
            // NOTE: Return ResultCode.InvalidArgument if an internal IPAddress is null, doesn't occur in our case.

            uint wirelessControllerRestriction = context.RequestData.ReadUInt32();

            if (wirelessControllerRestriction > 1)
            {
                return ResultCode.InvalidArgument;
            }

            if (_state != NetworkState.Initialized)
            {
                return ResultCode.InvalidState;
            }

            // NOTE: WirelessControllerRestriction value is used for the btm service in SetWlanMode call.
            //       Since we use our own implementation we can do nothing here.

            return ResultCode.Success;
        }

        [CommandCmif(200)]
        // OpenAccessPoint()
        public ResultCode OpenAccessPoint(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (_state != NetworkState.Initialized)
            {
                return ResultCode.InvalidState;
            }

            CloseStation();

            SetState(NetworkState.AccessPoint);

            _accessPoint = new AccessPoint(this);

            // NOTE: Calls nifm service and return related result codes.
            //       Since we use our own implementation we can return ResultCode.Success.

            return ResultCode.Success;
        }

        [CommandCmif(201)]
        // CloseAccessPoint()
        public ResultCode CloseAccessPoint(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (_state == NetworkState.AccessPoint || _state == NetworkState.AccessPointCreated)
            {
                DestroyNetworkImpl(DisconnectReason.DestroyedByUser);
            }
            else
            {
                return ResultCode.InvalidState;
            }

            SetState(NetworkState.Initialized);

            return ResultCode.Success;
        }

        private void CloseAccessPoint()
        {
            _accessPoint?.Dispose();
            _accessPoint = null;
        }

        [CommandCmif(202)]
        // CreateNetwork(bytes<0x44, 2> security_config, bytes<0x30, 1> user_config, bytes<0x20, 8> network_config)
        public ResultCode CreateNetwork(ServiceCtx context)
        {
            return CreateNetworkImpl(context);
        }

        [CommandCmif(203)]
        // CreateNetworkPrivate(bytes<0x44, 2> security_config, bytes<0x20, 1> security_parameter, bytes<0x30, 1>, bytes<0x20, 8> network_config, buffer<unknown, 9> address_entry, int count)
        public ResultCode CreateNetworkPrivate(ServiceCtx context)
        {
            return CreateNetworkImpl(context, true);
        }

        public ResultCode CreateNetworkImpl(ServiceCtx context, bool isPrivate = false)
        {
            SecurityConfig securityConfig = context.RequestData.ReadStruct<SecurityConfig>();
            SecurityParameter securityParameter = isPrivate ? context.RequestData.ReadStruct<SecurityParameter>() : new SecurityParameter();

            UserConfig userConfig = context.RequestData.ReadStruct<UserConfig>();

            context.RequestData.BaseStream.Seek(4, SeekOrigin.Current); // Alignment?
            NetworkConfig networkConfig = context.RequestData.ReadStruct<NetworkConfig>();

            if (networkConfig.IntentId.LocalCommunicationId == -1 && NetworkClient.NeedsRealId)
            {
                // TODO: Call nn::arp::GetApplicationControlProperty here when implemented.
                ApplicationControlProperty controlProperty = context.Device.Processes.ActiveApplication.ApplicationControlProperties;

                networkConfig.IntentId.LocalCommunicationId = (long)controlProperty.LocalCommunicationId[0];
            }

            bool isLocalCommunicationIdValid = CheckLocalCommunicationIdPermission(context, (ulong)networkConfig.IntentId.LocalCommunicationId);
            if (!isLocalCommunicationIdValid && NetworkClient.NeedsRealId)
            {
                return ResultCode.InvalidObject;
            }

            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            networkConfig.Channel = CheckDevelopmentChannel(networkConfig.Channel);
            securityConfig.SecurityMode = CheckDevelopmentSecurityMode(securityConfig.SecurityMode);

            if (networkConfig.NodeCountMax <= LdnConst.NodeCountMax)
            {
                if ((((ulong)networkConfig.LocalCommunicationVersion) & 0x80000000) == 0)
                {
                    if (securityConfig.SecurityMode <= SecurityMode.Retail)
                    {
                        if (securityConfig.Passphrase.Length <= LdnConst.PassphraseLengthMax)
                        {
                            if (_state == NetworkState.AccessPoint)
                            {
                                if (isPrivate)
                                {
                                    ulong bufferPosition = context.Request.PtrBuff[0].Position;
                                    ulong bufferSize = context.Request.PtrBuff[0].Size;

                                    byte[] addressListBytes = new byte[bufferSize];

                                    context.Memory.Read(bufferPosition, addressListBytes);

                                    AddressList addressList = MemoryMarshal.Cast<byte, AddressList>(addressListBytes)[0];

                                    _accessPoint.CreateNetworkPrivate(securityConfig, securityParameter, userConfig, networkConfig, addressList);
                                }
                                else
                                {
                                    _accessPoint.CreateNetwork(securityConfig, userConfig, networkConfig);
                                }

                                return ResultCode.Success;
                            }
                            else
                            {
                                return ResultCode.InvalidState;
                            }
                        }
                    }
                }
            }

            return ResultCode.InvalidArgument;
        }

        [CommandCmif(204)]
        // DestroyNetwork()
        public ResultCode DestroyNetwork(ServiceCtx context)
        {
            return DestroyNetworkImpl(DisconnectReason.DestroyedByUser);
        }

        private ResultCode DestroyNetworkImpl(DisconnectReason disconnectReason)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (disconnectReason - 3 <= DisconnectReason.DisconnectedByUser)
            {
                if (_state == NetworkState.AccessPointCreated)
                {
                    CloseAccessPoint();

                    SetState(NetworkState.AccessPoint);

                    return ResultCode.Success;
                }

                CloseAccessPoint();

                return ResultCode.InvalidState;
            }

            return ResultCode.InvalidArgument;
        }

        [CommandCmif(205)]
        // Reject(u32 node_id)
        public ResultCode Reject(ServiceCtx context)
        {
            uint nodeId = context.RequestData.ReadUInt32();

            return RejectImpl(DisconnectReason.Rejected, nodeId);
        }

        private ResultCode RejectImpl(DisconnectReason disconnectReason, uint nodeId)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (_state != NetworkState.AccessPointCreated)
            {
                return ResultCode.InvalidState; // Must be network host to reject nodes.
            }

            return NetworkClient.Reject(disconnectReason, nodeId);
        }

        [CommandCmif(206)]
        // SetAdvertiseData(buffer<advertise_data, 0x21>)
        public ResultCode SetAdvertiseData(ServiceCtx context)
        {
            (ulong bufferPosition, ulong bufferSize) = context.Request.GetBufferType0x21(0);

            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (bufferSize == 0 || bufferSize > LdnConst.AdvertiseDataSizeMax)
            {
                return ResultCode.InvalidArgument;
            }

            if (_state == NetworkState.AccessPoint || _state == NetworkState.AccessPointCreated)
            {
                byte[] advertiseData = new byte[bufferSize];

                context.Memory.Read(bufferPosition, advertiseData);

                return _accessPoint.SetAdvertiseData(advertiseData);
            }
            else
            {
                return ResultCode.InvalidState;
            }
        }

        [CommandCmif(207)]
        // SetStationAcceptPolicy(u8 accept_policy)
        public ResultCode SetStationAcceptPolicy(ServiceCtx context)
        {
            AcceptPolicy acceptPolicy = (AcceptPolicy)context.RequestData.ReadByte();

            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (acceptPolicy > AcceptPolicy.WhiteList)
            {
                return ResultCode.InvalidArgument;
            }

            if (_state == NetworkState.AccessPoint || _state == NetworkState.AccessPointCreated)
            {
                return _accessPoint.SetStationAcceptPolicy(acceptPolicy);
            }
            else
            {
                return ResultCode.InvalidState;
            }
        }

        [CommandCmif(208)]
        // AddAcceptFilterEntry(bytes<6, 1> mac_address)
        public ResultCode AddAcceptFilterEntry(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            // TODO

            return ResultCode.Success;
        }

        [CommandCmif(209)]
        // ClearAcceptFilter()
        public ResultCode ClearAcceptFilter(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            // TODO

            return ResultCode.Success;
        }

        [CommandCmif(300)]
        // OpenStation()
        public ResultCode OpenStation(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (_state != NetworkState.Initialized)
            {
                return ResultCode.InvalidState;
            }

            CloseAccessPoint();

            SetState(NetworkState.Station);

            _station?.Dispose();
            _station = new Station(this);

            // NOTE: Calls nifm service and returns related result codes.
            //       Since we use our own implementation we can return ResultCode.Success.

            return ResultCode.Success;
        }

        [CommandCmif(301)]
        // CloseStation()
        public ResultCode CloseStation(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (_state == NetworkState.Station || _state == NetworkState.StationConnected)
            {
                DisconnectImpl(DisconnectReason.DisconnectedByUser);
            }
            else
            {
                return ResultCode.InvalidState;
            }

            SetState(NetworkState.Initialized);

            return ResultCode.Success;
        }

        private void CloseStation()
        {
            _station?.Dispose();
            _station = null;
        }

        [CommandCmif(302)]
        // Connect(bytes<0x44, 2> security_config, bytes<0x30, 1> user_config, u32 local_communication_version, u32 option_unknown, buffer<network_info<0x480>, 0x19>)
        public ResultCode Connect(ServiceCtx context)
        {
            return ConnectImpl(context);
        }

        [CommandCmif(303)]
        // ConnectPrivate(bytes<0x44, 2> security_config, bytes<0x20, 1> security_parameter, bytes<0x30, 1> user_config, u32 local_communication_version, u32 option_unknown, bytes<0x20, 8> network_config)
        public ResultCode ConnectPrivate(ServiceCtx context)
        {
            return ConnectImpl(context, true);
        }

        private ResultCode ConnectImpl(ServiceCtx context, bool isPrivate = false)
        {
            SecurityConfig securityConfig = context.RequestData.ReadStruct<SecurityConfig>();
            SecurityParameter securityParameter = isPrivate ? context.RequestData.ReadStruct<SecurityParameter>() : new SecurityParameter();

            UserConfig userConfig = context.RequestData.ReadStruct<UserConfig>();
            uint localCommunicationVersion = context.RequestData.ReadUInt32();
            uint optionUnknown = context.RequestData.ReadUInt32();

            NetworkConfig networkConfig = new();
            NetworkInfo networkInfo = new();

            if (isPrivate)
            {
                context.RequestData.ReadUInt32(); // Padding.

                networkConfig = context.RequestData.ReadStruct<NetworkConfig>();
            }
            else
            {
                ulong bufferPosition = context.Request.PtrBuff[0].Position;
                ulong bufferSize = context.Request.PtrBuff[0].Size;

                byte[] networkInfoBytes = new byte[bufferSize];

                context.Memory.Read(bufferPosition, networkInfoBytes);

                networkInfo = MemoryMarshal.Read<NetworkInfo>(networkInfoBytes);
            }

            if (networkInfo.NetworkId.IntentId.LocalCommunicationId == -1 && NetworkClient.NeedsRealId)
            {
                // TODO: Call nn::arp::GetApplicationControlProperty here when implemented.
                ApplicationControlProperty controlProperty = context.Device.Processes.ActiveApplication.ApplicationControlProperties;

                networkInfo.NetworkId.IntentId.LocalCommunicationId = (long)controlProperty.LocalCommunicationId[0];
            }

            bool isLocalCommunicationIdValid = CheckLocalCommunicationIdPermission(context, (ulong)networkInfo.NetworkId.IntentId.LocalCommunicationId);
            if (!isLocalCommunicationIdValid && NetworkClient.NeedsRealId)
            {
                return ResultCode.InvalidObject;
            }

            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            securityConfig.SecurityMode = CheckDevelopmentSecurityMode(securityConfig.SecurityMode);

            ResultCode resultCode = ResultCode.InvalidArgument;

            if (securityConfig.SecurityMode - 1 <= SecurityMode.Debug)
            {
                if (optionUnknown <= 1 && (localCommunicationVersion >> 15) == 0 && securityConfig.PassphraseSize <= 64)
                {
                    resultCode = ResultCode.VersionTooLow;
                    if (localCommunicationVersion >= 0)
                    {
                        resultCode = ResultCode.VersionTooHigh;
                        if (localCommunicationVersion <= short.MaxValue)
                        {
                            if (_state != NetworkState.Station)
                            {
                                resultCode = ResultCode.InvalidState;
                            }
                            else
                            {
                                if (isPrivate)
                                {
                                    resultCode = _station.ConnectPrivate(securityConfig, securityParameter, userConfig, localCommunicationVersion, optionUnknown, networkConfig);
                                }
                                else
                                {
                                    resultCode = _station.Connect(securityConfig, userConfig, localCommunicationVersion, optionUnknown, networkInfo);
                                }
                            }
                        }
                    }
                }
            }

            return resultCode;
        }

        [CommandCmif(304)]
        // Disconnect()
        public ResultCode Disconnect(ServiceCtx context)
        {
            return DisconnectImpl(DisconnectReason.DisconnectedByUser);
        }

        private ResultCode DisconnectImpl(DisconnectReason disconnectReason)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (disconnectReason <= DisconnectReason.DisconnectedBySystem)
            {
                if (_state == NetworkState.StationConnected)
                {
                    SetState(NetworkState.Station);

                    CloseStation();

                    _disconnectReason = disconnectReason;

                    return ResultCode.Success;
                }

                CloseStation();

                return ResultCode.InvalidState;
            }

            return ResultCode.InvalidArgument;
        }

        [CommandCmif(400)]
        // InitializeOld(pid)
        public ResultCode InitializeOld(ServiceCtx context)
        {
            return InitializeImpl(context, context.Process.Pid, NifmRequestID);
        }

        [CommandCmif(401)]
        // Finalize()
        public ResultCode Finalize(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            // NOTE: Use true when its called in nn::ldn::detail::ISystemLocalCommunicationService
            ResultCode resultCode = FinalizeImpl(false);
            if (resultCode == ResultCode.Success)
            {
                SetDisconnectReason(DisconnectReason.None);
            }

            if (_stateChangeEventHandle != 0)
            {
                context.Process.HandleTable.CloseHandle(_stateChangeEventHandle);
                _stateChangeEventHandle = 0;
            }

            return resultCode;
        }

        private ResultCode FinalizeImpl(bool isCausedBySystem)
        {
            DisconnectReason disconnectReason;

            switch (_state)
            {
                case NetworkState.None:
                    return ResultCode.Success;
                case NetworkState.AccessPoint:
                    {
                        CloseAccessPoint();

                        break;
                    }
                case NetworkState.AccessPointCreated:
                    {
                        if (isCausedBySystem)
                        {
                            disconnectReason = DisconnectReason.DestroyedBySystem;
                        }
                        else
                        {
                            disconnectReason = DisconnectReason.DestroyedByUser;
                        }

                        DestroyNetworkImpl(disconnectReason);

                        break;
                    }
                case NetworkState.Station:
                    {
                        CloseStation();

                        break;
                    }
                case NetworkState.StationConnected:
                    {
                        if (isCausedBySystem)
                        {
                            disconnectReason = DisconnectReason.DisconnectedBySystem;
                        }
                        else
                        {
                            disconnectReason = DisconnectReason.DisconnectedByUser;
                        }

                        DisconnectImpl(disconnectReason);

                        break;
                    }
            }

            SetState(NetworkState.None);

            NetworkClient?.Dispose();
            NetworkClient = null;

            return ResultCode.Success;
        }

        [CommandCmif(402)] // 7.0.0+
        // Initialize(u64 ip_addresses, pid)
        public ResultCode Initialize(ServiceCtx context)
        {
            _ = new IPAddress(context.RequestData.ReadUInt32());
            _ = new IPAddress(context.RequestData.ReadUInt32());

            // NOTE: It seems the guest can get ip_address and subnet_mask from nifm service and pass it through the initialize.
            //       This calls InitializeImpl() twice: The first time with NIFM_REQUEST_ID, and if it fails, a second time with nifm_request_id = 1.

            return InitializeImpl(context, context.Process.Pid, NifmRequestID);
        }

        public ResultCode InitializeImpl(ServiceCtx context, ulong pid, int nifmRequestId)
        {
            ResultCode resultCode = ResultCode.InvalidArgument;

            if (nifmRequestId <= 255)
            {
                if (_state != NetworkState.Initialized)
                {
                    // NOTE: Service calls nn::ldn::detail::NetworkInterfaceManager::NetworkInterfaceMonitor::Initialize() with nifmRequestId as argument,
                    //       then it stores the result code of it in a global variable. Since we use our own implementation, we can just check the connection
                    //       and return related error codes.
                    if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    {
                        MultiplayerMode mode = context.Device.Configuration.MultiplayerMode;

                        Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"Initializing with multiplayer mode: {mode}");

                        switch (mode)
                        {
                            case MultiplayerMode.LdnRyu:
                                try
                                {
                                    string ldnServer = context.Device.Configuration.MultiplayerLdnServer 
                                                       ?? throw new InvalidOperationException("Cannot initialize RyuLDN with a null Multiplayer server.");

                                    if (!IPAddress.TryParse(ldnServer, out IPAddress ipAddress))
                                    {
                                        ipAddress = Dns.GetHostEntry(ldnServer).AddressList[0];
                                    }
                                    NetworkClient = new LdnMasterProxyClient(ipAddress.ToString(), SharedConstants.LanPlayPort, context.Device.Configuration);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error?.Print(LogClass.ServiceLdn, "Could not locate RyuLDN server. Defaulting to stubbed wireless.");
                                    Logger.Error?.Print(LogClass.ServiceLdn, ex.Message);
                                    NetworkClient = new LdnDisabledClient();
                                }
                                break;
                            case MultiplayerMode.LdnMitm:
                                NetworkClient = new LdnMitmClient(context.Device.Configuration);
                                break;
                            case MultiplayerMode.Disabled:
                                NetworkClient = new LdnDisabledClient();
                                break;
                        }

                        // TODO: Call nn::arp::GetApplicationLaunchProperty here when implemented.
                        NetworkClient.SetGameVersion(context.Device.Processes.ActiveApplication.ApplicationControlProperties.DisplayVersion.Items.ToArray());

                        resultCode = ResultCode.Success;

                        _nifmResultCode = resultCode;

                        SetState(NetworkState.Initialized);
                    }
                    else
                    {
                        // NOTE: Service returns different ResultCode here related to the nifm ResultCode.
                        resultCode = ResultCode.DeviceDisabled;
                        _nifmResultCode = resultCode;
                    }
                }
            }

            return resultCode;
        }

        public void Dispose()
        {
            _station?.Dispose();
            _station = null;

            _accessPoint?.Dispose();
            _accessPoint = null;

            NetworkClient?.DisconnectAndStop();
            NetworkClient = null;
        }
    }
}
