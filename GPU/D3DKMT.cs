using System;
using System.Runtime.InteropServices;


namespace GCMonitor
{
    #region struct

    //ULONG  =>  UInt32 
    // ULONGLONG  => UInt64

    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_ENUMADAPTERS
    {
        public UInt32 NumAdapters;
        public D3DKMT_ADAPTERINFO Adapter1;
        public D3DKMT_ADAPTERINFO Adapter2;
        public D3DKMT_ADAPTERINFO Adapter3;
        public D3DKMT_ADAPTERINFO Adapter4;
        public D3DKMT_ADAPTERINFO Adapter5;
        public D3DKMT_ADAPTERINFO Adapter6;
        public D3DKMT_ADAPTERINFO Adapter7;
        public D3DKMT_ADAPTERINFO Adapter8;
        public D3DKMT_ADAPTERINFO Adapter9;
        public D3DKMT_ADAPTERINFO Adapter10;
        public D3DKMT_ADAPTERINFO Adapter11;
        public D3DKMT_ADAPTERINFO Adapter12;
        public D3DKMT_ADAPTERINFO Adapter13;
        public D3DKMT_ADAPTERINFO Adapter14;
        public D3DKMT_ADAPTERINFO Adapter15;
        public D3DKMT_ADAPTERINFO Adapter16;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_ADAPTERINFO
    {
        public UInt32 hAdapter; // D3DKMT_HANDLE
        public LUID AdapterLuid;
        public UInt32 NumOfSources;
        public UInt32 bPresentMoveRegionsPreferred; // bolean
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public UInt32 LowPart;
        public UInt32 HighPart;
    }
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct LARGE_INTEGER
    {
        [FieldOffset(0)]
        public Int64 QuadPart;
        [FieldOffset(0)]
        public UInt32 LowPart;
        [FieldOffset(4)]
        public Int32 HighPart;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_OPENADAPTERFROMDEVICENAME
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDeviceName; //in PCWSTR
        public UInt32 hAdapter; //out D3DKMT_HANDLE
        public LUID AdapterLuid; //out
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_CLOSEADAPTER
    {
        public UInt32 hAdapter; //in D3DKMT_HANDLE
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATISTICS_COUNTER
    {
        public UInt32 Count;
        public UInt64 Bytes;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE_INFORMATION
    {
        public UInt32 PacketSubmited;
        public UInt32 PacketCompleted;
        public UInt32 PacketPreempted;
        public UInt32 PacketFaulted;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION
    {
        public UInt32 PacketSubmited;
        public UInt32 PacketCompleted;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATISTICS_PACKET_INFORMATION
    {
       // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] //D3DKMT_QueuePacketTypeMax BUG
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket_1;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket_2;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket_3;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket_4;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket_5;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket_6;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket_7;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket_8;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] //D3DKMT_DmaPacketTypeMax BUG
        //public D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE_INFORMATION[] DmaPacket;
        public D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE_INFORMATION DmaPacket_1;
        public D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE_INFORMATION DmaPacket_2;
        public D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE_INFORMATION DmaPacket_3;
        public D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE_INFORMATION DmaPacket_4;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct D3DKMT_QUERYSTATISTICS_PREEMPTION_INFORMATION //OK
    {
        public fixed UInt32 PreemptionCounter[(int)D3DKMT_QUERYRESULT_PREEMPTION_ATTEMPT_RESULT.D3DKMT_PreemptionAttemptStatisticsMax]; //[D3DKMT_PreemptionAttemptStatisticsMax]
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct D3DKMT_QUERYSTATISTICS_PROCESS_NODE_INFORMATION
    {
        public LARGE_INTEGER RunningTime; // 100ns
        public UInt32 ContextSwitch;
        public D3DKMT_QUERYSTATISTICS_PREEMPTION_INFORMATION PreemptionStatistics;
        public D3DKMT_QUERYSTATISTICS_PACKET_INFORMATION PacketStatistics;
        public fixed UInt64 Reserved[8];
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct D3DKMT_QUERYSTATISTICS_NODE_INFORMATION
    {
        public D3DKMT_QUERYSTATISTICS_PROCESS_NODE_INFORMATION GlobalInformation; // global
        public D3DKMT_QUERYSTATISTICS_PROCESS_NODE_INFORMATION SystemInformation; // system thread
        public fixed UInt64 Reserved[8];
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct D3DKMT_QUERYSTATISTICS_PROCESS_VIDPNSOURCE_INFORMATION
    {
        public UInt32 Frame;
        public UInt32 CancelledFrame;
        public UInt32 QueuedPresent;
        public fixed UInt64 Reserved[8];
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct D3DKMT_QUERYSTATISTICS_VIDPNSOURCE_INFORMATION
    { 
        public D3DKMT_QUERYSTATISTICS_PROCESS_VIDPNSOURCE_INFORMATION GlobalInformation; // global
        public D3DKMT_QUERYSTATISTICS_PROCESS_VIDPNSOURCE_INFORMATION SystemInformation; // system thread
        public fixed UInt64 Reserved[8];
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATSTICS_REFERENCE_DMA_BUFFER
    {
        public UInt32 NbCall;
        public UInt32 NbAllocationsReferenced;
        public UInt32 MaxNbAllocationsReferenced;
        public UInt32 NbNULLReference;
        public UInt32 NbWriteReference;
        public UInt32 NbRenamedAllocationsReferenced;
        public UInt32 NbIterationSearchingRenamedAllocation;
        public UInt32 NbLockedAllocationReferenced;
        public UInt32 NbAllocationWithValidPrepatchingInfoReferenced;
        public UInt32 NbAllocationWithInvalidPrepatchingInfoReferenced;
        public UInt32 NbDMABufferSuccessfullyPrePatched;
        public UInt32 NbPrimariesReferencesOverflow;
        public UInt32 NbAllocationWithNonPreferredResources;
        public UInt32 NbAllocationInsertedInMigrationTable;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATSTICS_RENAMING
    {
        public UInt32 NbAllocationsRenamed;
        public UInt32 NbAllocationsShrinked;
        public UInt32 NbRenamedBuffer;
        public UInt32 MaxRenamingListLength;
        public UInt32 NbFailuresDueToRenamingLimit;
        public UInt32 NbFailuresDueToCreateAllocation;
        public UInt32 NbFailuresDueToOpenAllocation;
        public UInt32 NbFailuresDueToLowResource;
        public UInt32 NbFailuresDueToNonRetiredLimit;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATSTICS_PREPRATION
    {
        public UInt32 BroadcastStall;
        public UInt32 NbDMAPrepared;
        public UInt32 NbDMAPreparedLongPath;
        public UInt32 ImmediateHighestPreparationPass;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocationsTrimmed;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATSTICS_PAGING_FAULT
    {
        public D3DKMT_QUERYSTATISTICS_COUNTER Faults;
        public D3DKMT_QUERYSTATISTICS_COUNTER FaultsFirstTimeAccess;
        public D3DKMT_QUERYSTATISTICS_COUNTER FaultsReclaimed;
        public D3DKMT_QUERYSTATISTICS_COUNTER FaultsMigration;
        public D3DKMT_QUERYSTATISTICS_COUNTER FaultsIncorrectResource;
        public D3DKMT_QUERYSTATISTICS_COUNTER FaultsLostContent;
        public D3DKMT_QUERYSTATISTICS_COUNTER FaultsEvicted;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocationsMEM_RESET;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocationsUnresetSuccess;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocationsUnresetFail;
        public UInt32 AllocationsUnresetSuccessRead;
        public UInt32 AllocationsUnresetFailRead;
        public D3DKMT_QUERYSTATISTICS_COUNTER Evictions;
        public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToPreparation;
        public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToLock;
        public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToClose;
        public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToPurge;
        public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToSuspendCPUAccess;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATSTICS_PAGING_TRANSFER
    {
        public UInt64 BytesFilled;
        public UInt64 BytesDiscarded;
        public UInt64 BytesMappedIntoAperture;
        public UInt64 BytesUnmappedFromAperture;
        public UInt64 BytesTransferredFromMdlToMemory;
        public UInt64 BytesTransferredFromMemoryToMdl;
        public UInt64 BytesTransferredFromApertureToMemory;
        public UInt64 BytesTransferredFromMemoryToAperture;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATSTICS_SWIZZLING_RANGE
    {
        public UInt32 NbRangesAcquired;
        public UInt32 NbRangesReleased;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATSTICS_LOCKS
    {
        public UInt32 NbLocks;
        public UInt32 NbLocksWaitFlag;
        public UInt32 NbLocksDiscardFlag;
        public UInt32 NbLocksNoOverwrite;
        public UInt32 NbLocksNoReadSync;
        public UInt32 NbLocksLinearization;
        public UInt32 NbComplexLocks;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATSTICS_ALLOCATIONS
    {
        public D3DKMT_QUERYSTATISTICS_COUNTER Created;
        public D3DKMT_QUERYSTATISTICS_COUNTER Destroyed;
        public D3DKMT_QUERYSTATISTICS_COUNTER Opened;
        public D3DKMT_QUERYSTATISTICS_COUNTER Closed;
        public D3DKMT_QUERYSTATISTICS_COUNTER MigratedSuccess;
        public D3DKMT_QUERYSTATISTICS_COUNTER MigratedFail;
        public D3DKMT_QUERYSTATISTICS_COUNTER MigratedAbandoned;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATSTICS_TERMINATIONS
    {
        public D3DKMT_QUERYSTATISTICS_COUNTER TerminatedShared;
        public D3DKMT_QUERYSTATISTICS_COUNTER TerminatedNonShared;
        public D3DKMT_QUERYSTATISTICS_COUNTER DestroyedShared;
        public D3DKMT_QUERYSTATISTICS_COUNTER DestroyedNonShared;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION
    {
        public UInt32 NbSegments;
        
        public UInt32 NodeCount;
        public UInt32 VidPnSourceCount;
        public Int32 VSyncEnabled;
        public UInt32 TdrDetectedCount;
        public Int64 ZeroLengthDmaBuffers;
        public UInt64 RestartedPeriod;
        
        public D3DKMT_QUERYSTATSTICS_REFERENCE_DMA_BUFFER ReferenceDmaBuffer;
        public D3DKMT_QUERYSTATSTICS_RENAMING Renaming;
        public D3DKMT_QUERYSTATSTICS_PREPRATION Preparation;
        public D3DKMT_QUERYSTATSTICS_PAGING_FAULT PagingFault;
        public D3DKMT_QUERYSTATSTICS_PAGING_TRANSFER PagingTransfer;
        public D3DKMT_QUERYSTATSTICS_SWIZZLING_RANGE SwizzlingRange;
        public D3DKMT_QUERYSTATSTICS_LOCKS Locks;
        public D3DKMT_QUERYSTATSTICS_ALLOCATIONS Allocations;
        public D3DKMT_QUERYSTATSTICS_TERMINATIONS Terminations;
        public fixed UInt64 Reserved[8];
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATISTICS_SYSTEM_MEMORY
    {
        public UInt64 BytesAllocated;
        public UInt64 BytesReserved;
        public UInt32 SmallAllocationBlocks;
        public UInt32 LargeAllocationBlocks;
        public UInt64 WriteCombinedBytesAllocated;
        public UInt64 WriteCombinedBytesReserved;
        public UInt64 CachedBytesAllocated;
        public UInt64 CachedBytesReserved;
        public UInt64 SectionBytesAllocated;
        public UInt64 SectionBytesReserved;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct D3DKMT_QUERYSTATISTICS_PROCESS_INFORMATION
    {
        public UInt32 NodeCount;
        public UInt32 VidPnSourceCount;
        public D3DKMT_QUERYSTATISTICS_SYSTEM_MEMORY SystemMemory;
        public fixed UInt64 Reserved[8];
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATISTICS_DMA_BUFFER
    {
        public D3DKMT_QUERYSTATISTICS_COUNTER Size;
        public UInt32 AllocationListBytes;
        public UInt32 PatchLocationListBytes;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct D3DKMT_QUERYSTATISTICS_COMMITMENT_DATA
    {
        public UInt64 TotalBytesEvictedFromProcess;
        public fixed UInt64 BytesBySegmentPreference[5]; //D3DKMT_QUERYSTATISTICS_SEGMENT_PREFERENCE_MAX
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct D3DKMT_QUERYSTATISTICS_POLICY
    {
        public fixed UInt64 PreferApertureForRead[(int)D3DKMT_QUERYSTATISTICS_ALLOCATION_PRIORITY_CLASS.D3DKMT_MaxAllocationPriorityClass]; //D3DKMT_MaxAllocationPriorityClass
        public fixed UInt64 PreferAperture[(int)D3DKMT_QUERYSTATISTICS_ALLOCATION_PRIORITY_CLASS.D3DKMT_MaxAllocationPriorityClass]; //D3DKMT_MaxAllocationPriorityClass
        public UInt64 MemResetOnPaging;
        public UInt64 RemovePagesFromWorkingSetOnPaging;
        public UInt64 MigrationEnabled;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct D3DKMT_QUERYSTATISTICS_PROCESS_ADAPTER_INFORMATION
    {
        public UInt32 NbSegments;
        public UInt32 NodeCount;
        public UInt32 VidPnSourceCount;
        public UInt32 VirtualMemoryUsage;
        public D3DKMT_QUERYSTATISTICS_DMA_BUFFER DmaBuffer;
        public D3DKMT_QUERYSTATISTICS_COMMITMENT_DATA CommitmentData;
        public D3DKMT_QUERYSTATISTICS_POLICY _Policy;
        public fixed UInt64 Reserved[8];
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATISTICS_MEMORY
    {
        public UInt64 TotalBytesEvicted;
        public UInt32 AllocsCommitted;
        public UInt32 AllocsResident;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct POWERFLAGS
    {
        private readonly UInt64 raw;
        public byte PreservedDuringStandby { get { return (byte)((raw >> 0) & 0x3); } }
        public byte PreservedDuringHibernate { get { return (byte)((raw >> 2) & 0x3); } }
        public byte PartiallyPreservedDuringHibernate { get { return (byte)((raw >> 4) & 0x3); } }
        public byte Reserved { get { return (byte)((raw >> 66) & 0x3FFFFFFFFFFFFFFF); } }

        public POWERFLAGS(byte PreservedDuringStandby, byte PreservedDuringHibernate, byte PartiallyPreservedDuringHibernate, byte Reserved)
        {
            //Contract.Requires(PreservedDuringStandby < 0x2);
            //Contract.Requires(PreservedDuringHibernate < 0x2);
            //Contract.Requires(PartiallyPreservedDuringHibernate < 0x2);
            raw = (ulong)PreservedDuringStandby | (ulong)(PreservedDuringHibernate << 2) | (ulong)(PartiallyPreservedDuringHibernate << 4) | (ulong)(Reserved << 66);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION_V1
    {
        public UInt32 CommitLimit;
        public UInt32 BytesCommitted;
        public UInt32 BytesResident;
        public D3DKMT_QUERYSTATISTICS_MEMORY Memory;
        public UInt32 Aperture; // boolean 
        public fixed UInt64 TotalBytesEvictedByPriority[(int)D3DKMT_QUERYSTATISTICS_ALLOCATION_PRIORITY_CLASS.D3DKMT_MaxAllocationPriorityClass]; //5
        public UInt64 SystemMemoryEndAddress;
        public POWERFLAGS PowerFlags;
        public fixed UInt64 Reserved[7];
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION
    {
        public UInt64 CommitLimit;
        public UInt64 BytesCommitted;
        public UInt64 BytesResident;
        public D3DKMT_QUERYSTATISTICS_MEMORY Memory;
        public UInt32 Aperture; // boolean
        public fixed UInt64 TotalBytesEvictedByPriority[(int)D3DKMT_QUERYSTATISTICS_ALLOCATION_PRIORITY_CLASS.D3DKMT_MaxAllocationPriorityClass]; //5
        public UInt64 SystemMemoryEndAddress;
        public POWERFLAGS PowerFlags;
        public fixed UInt64 Reserved[6]; 
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATISTICS_VIDEO_MEMORY
    {
        public UInt32 AllocsCommitted;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]//D3DKMT_QUERYSTATISTICS_SEGMENT_PREFERENCE_MAX BUG
        //public D3DKMT_QUERYSTATISTICS_COUNTER[] AllocsResidentInP;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentInP_1;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentInP_2;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentInP_3;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentInP_4;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentInP_5;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentInNonPreferred;
        public UInt64 TotalBytesEvictedDueToPreparation;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT_POLICY
    {
        public UInt64 UseMRU;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT_INFORMATION
    {
        public UInt64 BytesCommitted;
         public UInt64 MaximumWorkingSet;
        public UInt64 MinimumWorkingSet;
        public UInt32 NbReferencedAllocationEvictedInPeriod;
        public D3DKMT_QUERYSTATISTICS_VIDEO_MEMORY VideoMemory;
        public D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT_POLICY _Policy;
        fixed UInt64 Reserved[8];
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATISTICS_QUERY_SEGMENT
    {
        public UInt32 SegmentId;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATISTICS_QUERY_NODE
    {
        public UInt32 NodeId;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATISTICS_QUERY_VIDPNSOURCE
    {
        public UInt32 VidPnSourceId;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct D3DKMT_QUERYSTATISTICS_RESULT //union
    {
        [FieldOffset(0), MarshalAs(UnmanagedType.Struct)]
        public D3DKMT_QUERYSTATISTICS_PROCESS_VIDPNSOURCE_INFORMATION ProcessVidPnSourceInformation;
        [FieldOffset(0), MarshalAs(UnmanagedType.Struct)]
        public D3DKMT_QUERYSTATISTICS_PROCESS_INFORMATION ProcessInformation;
        [FieldOffset(0), MarshalAs(UnmanagedType.Struct)]
        public D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION SegmentInformation;
        [FieldOffset(0), MarshalAs(UnmanagedType.Struct)]
        public D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION_V1 SegmentInformationV1;
        [FieldOffset(0), MarshalAs(UnmanagedType.Struct)]
        public D3DKMT_QUERYSTATISTICS_PROCESS_NODE_INFORMATION ProcessNodeInformation;
        [FieldOffset(0), MarshalAs(UnmanagedType.Struct)]
        public D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT_INFORMATION ProcessSegmentInformation;
        [FieldOffset(0), MarshalAs(UnmanagedType.Struct)]
        public D3DKMT_QUERYSTATISTICS_VIDPNSOURCE_INFORMATION VidPnSourceInformation;
        [FieldOffset(0), MarshalAs(UnmanagedType.Struct)]
        public D3DKMT_QUERYSTATISTICS_PROCESS_ADAPTER_INFORMATION ProcessAdapterInformation;
        [FieldOffset(0), MarshalAs(UnmanagedType.Struct)]
        public D3DKMT_QUERYSTATISTICS_NODE_INFORMATION NodeInformation;
        [FieldOffset(0), MarshalAs(UnmanagedType.Struct)]
        public D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION AdapterInformation;
    }
    [StructLayoutAttribute(LayoutKind.Explicit)]
    public struct D3DKMT_QUERYSTATISTICS_QUERY_UNION
    {
        [FieldOffset(0)]
        public D3DKMT_QUERYSTATISTICS_QUERY_SEGMENT QuerySegment;
        [FieldOffset(0)]
        public D3DKMT_QUERYSTATISTICS_QUERY_SEGMENT QueryProcessSegment;
        [FieldOffset(0)]
        public D3DKMT_QUERYSTATISTICS_QUERY_NODE QueryNode;
        [FieldOffset(0)]
        public D3DKMT_QUERYSTATISTICS_QUERY_NODE QueryProcessNode;
        [FieldOffset(0)]
        public D3DKMT_QUERYSTATISTICS_QUERY_VIDPNSOURCE QueryVidPnSource;
        [FieldOffset(0)]
        public D3DKMT_QUERYSTATISTICS_QUERY_VIDPNSOURCE QueryProcessVidPnSource;
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct D3DKMT_QUERYSTATISTICS
    {
        public D3DKMT_QUERYSTATISTICS_TYPE Type;
        public LUID AdapterLuid;
        public System.IntPtr hProcess;
        public D3DKMT_QUERYSTATISTICS_RESULT QueryResult;
        public D3DKMT_QUERYSTATISTICS_QUERY_UNION QueryUnion;
    }
    public enum D3DKMT_QUERYRESULT_PREEMPTION_ATTEMPT_RESULT
    {
        D3DKMT_PreemptionAttempt = 0,
        D3DKMT_PreemptionAttemptSuccess = 1,
        D3DKMT_PreemptionAttemptMissNoCommand = 2,
        D3DKMT_PreemptionAttemptMissNotEnabled = 3,
        D3DKMT_PreemptionAttemptMissNextFence = 4,
        D3DKMT_PreemptionAttemptMissPagingCommand = 5,
        D3DKMT_PreemptionAttemptMissSplittedCommand = 6,
        D3DKMT_PreemptionAttemptMissFenceCommand = 7,
        D3DKMT_PreemptionAttemptMissRenderPendingFlip = 8,
        D3DKMT_PreemptionAttemptMissNotMakingProgress = 9,
        D3DKMT_PreemptionAttemptMissLessPriority = 10,
        D3DKMT_PreemptionAttemptMissRemainingQuantum = 11,
        D3DKMT_PreemptionAttemptMissRemainingPreemptionQuantum = 12,
        D3DKMT_PreemptionAttemptMissAlreadyPreempting = 13,
        D3DKMT_PreemptionAttemptMissGlobalBlock = 14,
        D3DKMT_PreemptionAttemptMissAlreadyRunning = 15,
        D3DKMT_PreemptionAttemptStatisticsMax
    }
    public enum D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE
    {
        D3DKMT_ClientRenderBuffer = 0,
        D3DKMT_ClientPagingBuffer = 1,
        D3DKMT_SystemPagingBuffer = 2,
        D3DKMT_SystemPreemptionBuffer = 3,
        D3DKMT_DmaPacketTypeMax
    }
    public enum D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE
    {
        D3DKMT_RenderCommandBuffer = 0,
        D3DKMT_DeferredCommandBuffer = 1,
        D3DKMT_SystemCommandBuffer = 2,
        D3DKMT_MmIoFlipCommandBuffer = 3,
        D3DKMT_WaitCommandBuffer = 4,
        D3DKMT_SignalCommandBuffer = 5,
        D3DKMT_DeviceCommandBuffer = 6,
        D3DKMT_SoftwareCommandBuffer = 7,
        D3DKMT_QueuePacketTypeMax
    }
    public enum D3DKMT_QUERYSTATISTICS_ALLOCATION_PRIORITY_CLASS
    {
        D3DKMT_AllocationPriorityClassMinimum = 0,
        D3DKMT_AllocationPriorityClassLow = 1,
        D3DKMT_AllocationPriorityClassNormal = 2,
        D3DKMT_AllocationPriorityClassHigh = 3,
        D3DKMT_AllocationPriorityClassMaximum = 4,
        D3DKMT_MaxAllocationPriorityClass
    }
    public enum D3DKMT_QUERYSTATISTICS_TYPE
    {
        D3DKMT_QUERYSTATISTICS_ADAPTER = 0,
        D3DKMT_QUERYSTATISTICS_PROCESS = 1,
        D3DKMT_QUERYSTATISTICS_PROCESS_ADAPTER = 2,
        D3DKMT_QUERYSTATISTICS_SEGMENT = 3,
        D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT = 4,
        D3DKMT_QUERYSTATISTICS_NODE = 5,
        D3DKMT_QUERYSTATISTICS_PROCESS_NODE = 6,
        D3DKMT_QUERYSTATISTICS_VIDPNSOURCE = 7,
        D3DKMT_QUERYSTATISTICS_PROCESS_VIDPNSOURCE = 8
    }
    public enum NtStatus : uint
    {
        // Success
        Success = 0x00000000,
        Wait0 = 0x00000000,
        Wait1 = 0x00000001,
        Wait2 = 0x00000002,
        Wait3 = 0x00000003,
        Wait63 = 0x0000003f,
        Abandoned = 0x00000080,
        AbandonedWait0 = 0x00000080,
        AbandonedWait1 = 0x00000081,
        AbandonedWait2 = 0x00000082,
        AbandonedWait3 = 0x00000083,
        AbandonedWait63 = 0x000000bf,
        UserApc = 0x000000c0,
        KernelApc = 0x00000100,
        Alerted = 0x00000101,
        Timeout = 0x00000102,
        Pending = 0x00000103,
        Reparse = 0x00000104,
        MoreEntries = 0x00000105,
        NotAllAssigned = 0x00000106,
        SomeNotMapped = 0x00000107,
        OpLockBreakInProgress = 0x00000108,
        VolumeMounted = 0x00000109,
        RxActCommitted = 0x0000010a,
        NotifyCleanup = 0x0000010b,
        NotifyEnumDir = 0x0000010c,
        NoQuotasForAccount = 0x0000010d,
        PrimaryTransportConnectFailed = 0x0000010e,
        PageFaultTransition = 0x00000110,
        PageFaultDemandZero = 0x00000111,
        PageFaultCopyOnWrite = 0x00000112,
        PageFaultGuardPage = 0x00000113,
        PageFaultPagingFile = 0x00000114,
        CrashDump = 0x00000116,
        ReparseObject = 0x00000118,
        NothingToTerminate = 0x00000122,
        ProcessNotInJob = 0x00000123,
        ProcessInJob = 0x00000124,
        ProcessCloned = 0x00000129,
        FileLockedWithOnlyReaders = 0x0000012a,
        FileLockedWithWriters = 0x0000012b,

        // Informational
        Informational = 0x40000000,
        ObjectNameExists = 0x40000000,
        ThreadWasSuspended = 0x40000001,
        WorkingSetLimitRange = 0x40000002,
        ImageNotAtBase = 0x40000003,
        RegistryRecovered = 0x40000009,

        // Warning
        Warning = 0x80000000,
        GuardPageViolation = 0x80000001,
        DatatypeMisalignment = 0x80000002,
        Breakpoint = 0x80000003,
        SingleStep = 0x80000004,
        BufferOverflow = 0x80000005,
        NoMoreFiles = 0x80000006,
        HandlesClosed = 0x8000000a,
        PartialCopy = 0x8000000d,
        DeviceBusy = 0x80000011,
        InvalidEaName = 0x80000013,
        EaListInconsistent = 0x80000014,
        NoMoreEntries = 0x8000001a,
        LongJump = 0x80000026,
        DllMightBeInsecure = 0x8000002b,

        // Error
        Error = 0xc0000000,
        Unsuccessful = 0xc0000001,
        NotImplemented = 0xc0000002,
        InvalidInfoClass = 0xc0000003,
        InfoLengthMismatch = 0xc0000004,
        AccessViolation = 0xc0000005,
        InPageError = 0xc0000006,
        PagefileQuota = 0xc0000007,
        InvalidHandle = 0xc0000008,
        BadInitialStack = 0xc0000009,
        BadInitialPc = 0xc000000a,
        InvalidCid = 0xc000000b,
        TimerNotCanceled = 0xc000000c,
        InvalidParameter = 0xc000000d,
        NoSuchDevice = 0xc000000e,
        NoSuchFile = 0xc000000f,
        InvalidDeviceRequest = 0xc0000010,
        EndOfFile = 0xc0000011,
        WrongVolume = 0xc0000012,
        NoMediaInDevice = 0xc0000013,
        NoMemory = 0xc0000017,
        NotMappedView = 0xc0000019,
        UnableToFreeVm = 0xc000001a,
        UnableToDeleteSection = 0xc000001b,
        IllegalInstruction = 0xc000001d,
        AlreadyCommitted = 0xc0000021,
        AccessDenied = 0xc0000022,
        BufferTooSmall = 0xc0000023,
        ObjectTypeMismatch = 0xc0000024,
        NonContinuableException = 0xc0000025,
        BadStack = 0xc0000028,
        NotLocked = 0xc000002a,
        NotCommitted = 0xc000002d,
        InvalidParameterMix = 0xc0000030,
        ObjectNameInvalid = 0xc0000033,
        ObjectNameNotFound = 0xc0000034,
        ObjectNameCollision = 0xc0000035,
        ObjectPathInvalid = 0xc0000039,
        ObjectPathNotFound = 0xc000003a,
        ObjectPathSyntaxBad = 0xc000003b,
        DataOverrun = 0xc000003c,
        DataLate = 0xc000003d,
        DataError = 0xc000003e,
        CrcError = 0xc000003f,
        SectionTooBig = 0xc0000040,
        PortConnectionRefused = 0xc0000041,
        InvalidPortHandle = 0xc0000042,
        SharingViolation = 0xc0000043,
        QuotaExceeded = 0xc0000044,
        InvalidPageProtection = 0xc0000045,
        MutantNotOwned = 0xc0000046,
        SemaphoreLimitExceeded = 0xc0000047,
        PortAlreadySet = 0xc0000048,
        SectionNotImage = 0xc0000049,
        SuspendCountExceeded = 0xc000004a,
        ThreadIsTerminating = 0xc000004b,
        BadWorkingSetLimit = 0xc000004c,
        IncompatibleFileMap = 0xc000004d,
        SectionProtection = 0xc000004e,
        EasNotSupported = 0xc000004f,
        EaTooLarge = 0xc0000050,
        NonExistentEaEntry = 0xc0000051,
        NoEasOnFile = 0xc0000052,
        EaCorruptError = 0xc0000053,
        FileLockConflict = 0xc0000054,
        LockNotGranted = 0xc0000055,
        DeletePending = 0xc0000056,
        CtlFileNotSupported = 0xc0000057,
        UnknownRevision = 0xc0000058,
        RevisionMismatch = 0xc0000059,
        InvalidOwner = 0xc000005a,
        InvalidPrimaryGroup = 0xc000005b,
        NoImpersonationToken = 0xc000005c,
        CantDisableMandatory = 0xc000005d,
        NoLogonServers = 0xc000005e,
        NoSuchLogonSession = 0xc000005f,
        NoSuchPrivilege = 0xc0000060,
        PrivilegeNotHeld = 0xc0000061,
        InvalidAccountName = 0xc0000062,
        UserExists = 0xc0000063,
        NoSuchUser = 0xc0000064,
        GroupExists = 0xc0000065,
        NoSuchGroup = 0xc0000066,
        MemberInGroup = 0xc0000067,
        MemberNotInGroup = 0xc0000068,
        LastAdmin = 0xc0000069,
        WrongPassword = 0xc000006a,
        IllFormedPassword = 0xc000006b,
        PasswordRestriction = 0xc000006c,
        LogonFailure = 0xc000006d,
        AccountRestriction = 0xc000006e,
        InvalidLogonHours = 0xc000006f,
        InvalidWorkstation = 0xc0000070,
        PasswordExpired = 0xc0000071,
        AccountDisabled = 0xc0000072,
        NoneMapped = 0xc0000073,
        TooManyLuidsRequested = 0xc0000074,
        LuidsExhausted = 0xc0000075,
        InvalidSubAuthority = 0xc0000076,
        InvalidAcl = 0xc0000077,
        InvalidSid = 0xc0000078,
        InvalidSecurityDescr = 0xc0000079,
        ProcedureNotFound = 0xc000007a,
        InvalidImageFormat = 0xc000007b,
        NoToken = 0xc000007c,
        BadInheritanceAcl = 0xc000007d,
        RangeNotLocked = 0xc000007e,
        DiskFull = 0xc000007f,
        ServerDisabled = 0xc0000080,
        ServerNotDisabled = 0xc0000081,
        TooManyGuidsRequested = 0xc0000082,
        GuidsExhausted = 0xc0000083,
        InvalidIdAuthority = 0xc0000084,
        AgentsExhausted = 0xc0000085,
        InvalidVolumeLabel = 0xc0000086,
        SectionNotExtended = 0xc0000087,
        NotMappedData = 0xc0000088,
        ResourceDataNotFound = 0xc0000089,
        ResourceTypeNotFound = 0xc000008a,
        ResourceNameNotFound = 0xc000008b,
        ArrayBoundsExceeded = 0xc000008c,
        FloatDenormalOperand = 0xc000008d,
        FloatDivideByZero = 0xc000008e,
        FloatInexactResult = 0xc000008f,
        FloatInvalidOperation = 0xc0000090,
        FloatOverflow = 0xc0000091,
        FloatStackCheck = 0xc0000092,
        FloatUnderflow = 0xc0000093,
        IntegerDivideByZero = 0xc0000094,
        IntegerOverflow = 0xc0000095,
        PrivilegedInstruction = 0xc0000096,
        TooManyPagingFiles = 0xc0000097,
        FileInvalid = 0xc0000098,
        InstanceNotAvailable = 0xc00000ab,
        PipeNotAvailable = 0xc00000ac,
        InvalidPipeState = 0xc00000ad,
        PipeBusy = 0xc00000ae,
        IllegalFunction = 0xc00000af,
        PipeDisconnected = 0xc00000b0,
        PipeClosing = 0xc00000b1,
        PipeConnected = 0xc00000b2,
        PipeListening = 0xc00000b3,
        InvalidReadMode = 0xc00000b4,
        IoTimeout = 0xc00000b5,
        FileForcedClosed = 0xc00000b6,
        ProfilingNotStarted = 0xc00000b7,
        ProfilingNotStopped = 0xc00000b8,
        NotSameDevice = 0xc00000d4,
        FileRenamed = 0xc00000d5,
        CantWait = 0xc00000d8,
        PipeEmpty = 0xc00000d9,
        CantTerminateSelf = 0xc00000db,
        InternalError = 0xc00000e5,
        InvalidParameter1 = 0xc00000ef,
        InvalidParameter2 = 0xc00000f0,
        InvalidParameter3 = 0xc00000f1,
        InvalidParameter4 = 0xc00000f2,
        InvalidParameter5 = 0xc00000f3,
        InvalidParameter6 = 0xc00000f4,
        InvalidParameter7 = 0xc00000f5,
        InvalidParameter8 = 0xc00000f6,
        InvalidParameter9 = 0xc00000f7,
        InvalidParameter10 = 0xc00000f8,
        InvalidParameter11 = 0xc00000f9,
        InvalidParameter12 = 0xc00000fa,
        MappedFileSizeZero = 0xc000011e,
        TooManyOpenedFiles = 0xc000011f,
        Cancelled = 0xc0000120,
        CannotDelete = 0xc0000121,
        InvalidComputerName = 0xc0000122,
        FileDeleted = 0xc0000123,
        SpecialAccount = 0xc0000124,
        SpecialGroup = 0xc0000125,
        SpecialUser = 0xc0000126,
        MembersPrimaryGroup = 0xc0000127,
        FileClosed = 0xc0000128,
        TooManyThreads = 0xc0000129,
        ThreadNotInProcess = 0xc000012a,
        TokenAlreadyInUse = 0xc000012b,
        PagefileQuotaExceeded = 0xc000012c,
        CommitmentLimit = 0xc000012d,
        InvalidImageLeFormat = 0xc000012e,
        InvalidImageNotMz = 0xc000012f,
        InvalidImageProtect = 0xc0000130,
        InvalidImageWin16 = 0xc0000131,
        LogonServer = 0xc0000132,
        DifferenceAtDc = 0xc0000133,
        SynchronizationRequired = 0xc0000134,
        DllNotFound = 0xc0000135,
        IoPrivilegeFailed = 0xc0000137,
        OrdinalNotFound = 0xc0000138,
        EntryPointNotFound = 0xc0000139,
        ControlCExit = 0xc000013a,
        PortNotSet = 0xc0000353,
        DebuggerInactive = 0xc0000354,
        CallbackBypass = 0xc0000503,
        PortClosed = 0xc0000700,
        MessageLost = 0xc0000701,
        InvalidMessage = 0xc0000702,
        RequestCanceled = 0xc0000703,
        RecursiveDispatch = 0xc0000704,
        LpcReceiveBufferExpected = 0xc0000705,
        LpcInvalidConnectionUsage = 0xc0000706,
        LpcRequestsNotAllowed = 0xc0000707,
        ResourceInUse = 0xc0000708,
        ProcessIsProtected = 0xc0000712,
        VolumeDirty = 0xc0000806,
        FileCheckedOut = 0xc0000901,
        CheckOutRequired = 0xc0000902,
        BadFileType = 0xc0000903,
        FileTooLarge = 0xc0000904,
        FormsAuthRequired = 0xc0000905,
        VirusInfected = 0xc0000906,
        VirusDeleted = 0xc0000907,
        TransactionalConflict = 0xc0190001,
        InvalidTransaction = 0xc0190002,
        TransactionNotActive = 0xc0190003,
        TmInitializationFailed = 0xc0190004,
        RmNotActive = 0xc0190005,
        RmMetadataCorrupt = 0xc0190006,
        TransactionNotJoined = 0xc0190007,
        DirectoryNotRm = 0xc0190008,
        CouldNotResizeLog = 0xc0190009,
        TransactionsUnsupportedRemote = 0xc019000a,
        LogResizeInvalidSize = 0xc019000b,
        RemoteFileVersionMismatch = 0xc019000c,
        CrmProtocolAlreadyExists = 0xc019000f,
        TransactionPropagationFailed = 0xc0190010,
        CrmProtocolNotFound = 0xc0190011,
        TransactionSuperiorExists = 0xc0190012,
        TransactionRequestNotValid = 0xc0190013,
        TransactionNotRequested = 0xc0190014,
        TransactionAlreadyAborted = 0xc0190015,
        TransactionAlreadyCommitted = 0xc0190016,
        TransactionInvalidMarshallBuffer = 0xc0190017,
        CurrentTransactionNotValid = 0xc0190018,
        LogGrowthFailed = 0xc0190019,
        ObjectNoLongerExists = 0xc0190021,
        StreamMiniversionNotFound = 0xc0190022,
        StreamMiniversionNotValid = 0xc0190023,
        MiniversionInaccessibleFromSpecifiedTransaction = 0xc0190024,
        CantOpenMiniversionWithModifyIntent = 0xc0190025,
        CantCreateMoreStreamMiniversions = 0xc0190026,
        HandleNoLongerValid = 0xc0190028,
        NoTxfMetadata = 0xc0190029,
        LogCorruptionDetected = 0xc0190030,
        CantRecoverWithHandleOpen = 0xc0190031,
        RmDisconnected = 0xc0190032,
        EnlistmentNotSuperior = 0xc0190033,
        RecoveryNotNeeded = 0xc0190034,
        RmAlreadyStarted = 0xc0190035,
        FileIdentityNotPersistent = 0xc0190036,
        CantBreakTransactionalDependency = 0xc0190037,
        CantCrossRmBoundary = 0xc0190038,
        TxfDirNotEmpty = 0xc0190039,
        IndoubtTransactionsExist = 0xc019003a,
        TmVolatile = 0xc019003b,
        RollbackTimerExpired = 0xc019003c,
        TxfAttributeCorrupt = 0xc019003d,
        EfsNotAllowedInTransaction = 0xc019003e,
        TransactionalOpenNotAllowed = 0xc019003f,
        TransactedMappingUnsupportedRemote = 0xc0190040,
        TxfMetadataAlreadyPresent = 0xc0190041,
        TransactionScopeCallbacksNotSet = 0xc0190042,
        TransactionRequiredPromotion = 0xc0190043,
        CannotExecuteFileInTransaction = 0xc0190044,
        TransactionsNotFrozen = 0xc0190045,

        MaximumNtStatus = 0xffffffff
    }

    #endregion

    #region class
    public static class D3DKMT
    {
        #region NtStatus

        public static bool Nt_Error(this NtStatus status)
        {
            return status >= NtStatus.Error && status <= NtStatus.MaximumNtStatus;
        }
        public static bool Nt_Informational(this NtStatus status)
        {
            return status >= NtStatus.Informational && status < NtStatus.Warning;
        }

        public static bool Nt_Success(this NtStatus status)
        {
            return status >= NtStatus.Success && status < NtStatus.Informational;
        }

        public static bool Nt_Warning(this NtStatus status)
        {
            return status >= NtStatus.Warning && status < NtStatus.Error;
        }
        #endregion

        [DllImport("gdi32.dll", EntryPoint = "D3DKMTOpenAdapterFromDeviceName", CharSet = CharSet.Unicode)]
        static public extern NtStatus D3DKMTOpenAdapterFromDeviceName(IntPtr OPENADAPTERFROMDEVICENAME);

        [DllImport("gdi32.dll", EntryPoint = "D3DKMTCloseAdapter", CharSet = CharSet.Unicode)]
        static public extern NtStatus D3DKMTCloseAdapter(IntPtr CLOSEADAPTER);

        [DllImport("gdi32.dll", EntryPoint = "D3DKMTQueryStatistics", CharSet = CharSet.Unicode)]
        static public extern NtStatus D3DKMTQueryStatistics(IntPtr QUERYSTATISTICS);


        [DllImport("gdi32.dll", EntryPoint = "D3DKMTEnumAdapters", CharSet = CharSet.Unicode)]
        static public extern NtStatus D3DKMTEnumAdapters(IntPtr D3DKMT_ENUMADAPTERS);

    }
    #endregion
}
