namespace ZenithUI;

/// <summary>
/// The names of every string in <c>ZenStrings.resx</c>, so a component asks for a key the
/// compiler has checked rather than a literal that fails silently by rendering its own name.
/// </summary>
/// <remarks>
/// Kept in step with the resource files by <c>ZenLocalizationTests</c>, which fails on a key
/// missing from a translation, a translation with no key, or placeholders that disagree.
/// Keys shared by several components start <c>Common_</c>; the rest are prefixed with the
/// component that renders them, so a translator can find the context.
/// </remarks>
internal static class ZenStringKeys
{
    // ---- Shared -------------------------------------------------------------------------------
    public const string Common_Cancel = nameof(Common_Cancel);
    public const string Common_ClearSelection = nameof(Common_ClearSelection);
    public const string Common_Close = nameof(Common_Close);
    public const string Common_Confirm = nameof(Common_Confirm);
    public const string Common_Dismiss = nameof(Common_Dismiss);
    public const string Common_Loading = nameof(Common_Loading);
    public const string Common_NoData = nameof(Common_NoData);
    public const string Common_NoMatches = nameof(Common_NoMatches);

    // ---- Shell --------------------------------------------------------------------------------
    public const string AppBar_OpenNavigation = nameof(AppBar_OpenNavigation);
    public const string AppShell_SkipToContent = nameof(AppShell_SkipToContent);
    public const string SideNav_Label = nameof(SideNav_Label);
    public const string SideNav_Close = nameof(SideNav_Close);
    public const string SideNav_Collapse = nameof(SideNav_Collapse);
    public const string SideNav_Expand = nameof(SideNav_Expand);

    // ---- Stepper ------------------------------------------------------------------------------
    public const string Stepper_Label = nameof(Stepper_Label);
    public const string Stepper_Back = nameof(Stepper_Back);
    public const string Stepper_Next = nameof(Stepper_Next);
    public const string Stepper_Finish = nameof(Stepper_Finish);
    public const string Stepper_Completed = nameof(Stepper_Completed);
    public const string Stepper_HasErrors = nameof(Stepper_HasErrors);
    public const string Stepper_Optional = nameof(Stepper_Optional);

    // ---- Data ---------------------------------------------------------------------------------
    public const string List_Empty = nameof(List_Empty);
    public const string Table_Details = nameof(Table_Details);
    public const string Table_DetailsFor = nameof(Table_DetailsFor);
    public const string Table_SelectAllOnPage = nameof(Table_SelectAllOnPage);
    public const string Table_SelectRow = nameof(Table_SelectRow);
    public const string Table_SelectGroup = nameof(Table_SelectGroup);
    public const string Table_PreviousPage = nameof(Table_PreviousPage);
    public const string Table_NextPage = nameof(Table_NextPage);
    public const string Table_PageNumber = nameof(Table_PageNumber);
    public const string Table_Pagination = nameof(Table_Pagination);
    public const string Table_PaginationFor = nameof(Table_PaginationFor);
    public const string Table_PageSummary = nameof(Table_PageSummary);
    public const string Table_NoRows = nameof(Table_NoRows);
    public const string Table_RowsPerPage = nameof(Table_RowsPerPage);
    public const string Table_RowOne = nameof(Table_RowOne);
    public const string Table_RowOther = nameof(Table_RowOther);
    public const string Table_Ungrouped = nameof(Table_Ungrouped);

    // ---- Forms --------------------------------------------------------------------------------
    public const string Calendar_Today = nameof(Calendar_Today);
    public const string Calendar_Clear = nameof(Calendar_Clear);
    public const string Calendar_PreviousMonth = nameof(Calendar_PreviousMonth);
    public const string Calendar_NextMonth = nameof(Calendar_NextMonth);
    public const string Combobox_Suggestions = nameof(Combobox_Suggestions);
    public const string Combobox_Searching = nameof(Combobox_Searching);
    public const string Combobox_ShowSuggestions = nameof(Combobox_ShowSuggestions);
    public const string Combobox_HideSuggestions = nameof(Combobox_HideSuggestions);
    public const string Combobox_Remove = nameof(Combobox_Remove);
    public const string DatePicker_ChooseDate = nameof(DatePicker_ChooseDate);
    public const string DatePicker_Choose = nameof(DatePicker_Choose);
    public const string DatePicker_OpenCalendar = nameof(DatePicker_OpenCalendar);
    public const string DatePicker_CloseCalendar = nameof(DatePicker_CloseCalendar);
    public const string DatePicker_EnterDateAs = nameof(DatePicker_EnterDateAs);
    public const string Form_SummaryTitle = nameof(Form_SummaryTitle);
    public const string Lookup_Results = nameof(Lookup_Results);
    public const string Lookup_TryDifferentSearch = nameof(Lookup_TryDifferentSearch);
    public const string Lookup_Selected = nameof(Lookup_Selected);
    public const string Lookup_NothingPending = nameof(Lookup_NothingPending);
    public const string Lookup_ShowResults = nameof(Lookup_ShowResults);
    public const string Lookup_HideResults = nameof(Lookup_HideResults);
    public const string SearchInput_Clear = nameof(SearchInput_Clear);
    public const string SearchInput_Busy = nameof(SearchInput_Busy);
    public const string Validation_Invalid = nameof(Validation_Invalid);
    public const string Validation_Required = nameof(Validation_Required);
    public const string Validation_InvalidDate = nameof(Validation_InvalidDate);
    public const string Validation_InvalidAmount = nameof(Validation_InvalidAmount);

    // ---- Surfaces and feedback ----------------------------------------------------------------
    public const string StatCard_Up = nameof(StatCard_Up);
    public const string StatCard_Down = nameof(StatCard_Down);
    public const string StatCard_Flat = nameof(StatCard_Flat);
    public const string SplitButton_MoreOptions = nameof(SplitButton_MoreOptions);

    // ---- Theming ------------------------------------------------------------------------------
    public const string ThemeToggle_Label = nameof(ThemeToggle_Label);
    public const string ThemeToggle_Light = nameof(ThemeToggle_Light);
    public const string ThemeToggle_Dark = nameof(ThemeToggle_Dark);
    public const string ThemeToggle_System = nameof(ThemeToggle_System);
    public const string ThemeToggle_SwitchTo = nameof(ThemeToggle_SwitchTo);
}
