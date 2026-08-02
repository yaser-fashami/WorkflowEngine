namespace Workflow.Domain.Enums;

public enum WorkflowStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public enum WorkflowInstanceStatus
{
    Draft = 1,
    Running = 2,
    Completed = 3,
    Rejected = 4,
    Cancelled = 5
}

public enum WorkflowNodeType
{
    Start,
    Task,
    Approval,
    End
}
public enum WorkflowTaskStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Rejected = 3,
    Cancelled = 4,
    Expired = 5
}

public enum WorkflowTaskType
{
    UserTask = 0,
    Approval = 1
}

public enum WorkflowActionType
{
    Approve = 0,
    Reject = 1,
    Complete = 2,
    Cancel = 3
}


public enum WorkflowEndType
{
    Success = 0,
    Rejected = 1,
    Cancelled = 2
}

public enum WorkflowVersionStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}

public enum WorkflowTaskAssignmentType
{
    User = 1,
    Role = 2,
    Department = 3
}

public enum WorkflowAssignmentType
{
    User = 1,
    Role = 2,
    Department = 3
}