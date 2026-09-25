import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../core/routes/app_router.dart';
import '../core/theme/app_colors.dart';
import '../providers/work_order_provider.dart';
import '../widgets/app_top_bar.dart';
import '../widgets/empty_state.dart';
import '../widgets/priority_badge.dart';
import '../widgets/status_badge.dart';

class AllJobsScreen extends StatefulWidget {
  const AllJobsScreen({super.key});

  @override
  State<AllJobsScreen> createState() => _AllJobsScreenState();
}

class _AllJobsScreenState extends State<AllJobsScreen> {
  String _selectedStatus = '';
  String _selectedPriority = '';
  final TextEditingController _searchController = TextEditingController();
  bool _showSearch = false;

  final List<Map<String, String>> _statusFilters = [
    {'label': 'All', 'value': ''},
    {'label': 'Approved', 'value': 'Approved'},
    {'label': 'Scheduled', 'value': 'Scheduled'},
    {'label': 'In Progress', 'value': 'InProgress'},
    {'label': 'Paused', 'value': 'Paused'},
    {'label': 'Completed', 'value': 'Completed'},
  ];

  final List<Map<String, String>> _priorityFilters = [
    {'label': 'All Priorities', 'value': ''},
    {'label': 'Critical', 'value': 'Critical'},
    {'label': 'High', 'value': 'High'},
    {'label': 'Medium', 'value': 'Medium'},
    {'label': 'Low', 'value': 'Low'},
  ];

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<WorkOrderProvider>().fetchAllJobs();
    });
    _searchController.addListener(_onSearchChanged);
  }

  @override
  void dispose() {
    _searchController.removeListener(_onSearchChanged);
    _searchController.dispose();
    super.dispose();
  }

  void _onSearchChanged() {
    context.read<WorkOrderProvider>().fetchAllJobs(
          status: _selectedStatus.isEmpty ? null : _selectedStatus,
          search: _searchController.text.trim().isEmpty ? null : _searchController.text.trim(),
          priority: _selectedPriority.isEmpty ? null : _selectedPriority,
        );
  }

  void _applyFilters() {
    context.read<WorkOrderProvider>().fetchAllJobs(
          status: _selectedStatus.isEmpty ? null : _selectedStatus,
          search: _searchController.text.trim().isEmpty ? null : _searchController.text.trim(),
          priority: _selectedPriority.isEmpty ? null : _selectedPriority,
        );
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<WorkOrderProvider>();
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      appBar: AppTopBar(
        title: 'All Jobs',
        onRefresh: _applyFilters,
        additionalActions: [
          IconButton(
            icon: Icon(_showSearch ? Icons.search_off : Icons.search),
            tooltip: 'Search Jobs',
            onPressed: () {
              setState(() {
                _showSearch = !_showSearch;
                if (!_showSearch) {
                  _searchController.clear();
                }
              });
            },
          ),
        ],
      ),
      body: Column(
        children: [
          // Search Bar
          if (_showSearch)
            Container(
              color: isDark ? AppColors.darkCard : Colors.white,
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
              child: TextField(
                controller: _searchController,
                autofocus: true,
                decoration: InputDecoration(
                  hintText: 'Search by job number or title...',
                  prefixIcon: const Icon(Icons.search, color: AppColors.primary),
                  suffixIcon: _searchController.text.isNotEmpty
                      ? IconButton(
                          icon: const Icon(Icons.close),
                          onPressed: () {
                            _searchController.clear();
                          },
                        )
                      : null,
                  isDense: true,
                  contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                ),
              ),
            ),

          // Status Filter Chips
          Container(
            color: isDark ? AppColors.darkCard : Colors.white,
            child: Column(
              children: [
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                  child: Row(
                    children: _statusFilters.map((filter) {
                      final isSelected = _selectedStatus == filter['value'];
                      return Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: FilterChip(
                          label: Text(
                            filter['label']!,
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
                              color: isSelected
                                  ? Colors.white
                                  : (isDark ? AppColors.darkTextPrimary : AppColors.lightTextPrimary),
                            ),
                          ),
                          selected: isSelected,
                          onSelected: (selected) {
                            setState(() {
                              _selectedStatus = selected ? filter['value']! : '';
                            });
                            _applyFilters();
                          },
                          selectedColor: AppColors.primary,
                          checkmarkColor: Colors.white,
                          backgroundColor: isDark ? AppColors.darkCard : const Color(0xFFF1F5F9),
                          side: BorderSide(
                            color: isSelected ? AppColors.primary : (isDark ? AppColors.darkBorder : AppColors.lightBorder),
                          ),
                        ),
                      );
                    }).toList(),
                  ),
                ),

                // Priority Filter Row
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  padding: const EdgeInsets.fromLTRB(12, 0, 12, 8),
                  child: Row(
                    children: _priorityFilters.map((filter) {
                      final isSelected = _selectedPriority == filter['value'];
                      final priorityColor = filter['value']!.isNotEmpty
                          ? AppColors.getPriorityColor(filter['value']!)
                          : (isDark ? AppColors.darkTextSecondary : AppColors.lightTextSecondary);

                      return Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: ChoiceChip(
                          label: Text(
                            filter['label']!,
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
                              color: isSelected ? Colors.white : priorityColor,
                            ),
                          ),
                          selected: isSelected,
                          onSelected: (selected) {
                            setState(() {
                              _selectedPriority = selected ? filter['value']! : '';
                            });
                            _applyFilters();
                          },
                          selectedColor: filter['value']!.isNotEmpty
                              ? AppColors.getPriorityColor(filter['value']!)
                              : AppColors.primary,
                          backgroundColor: filter['value']!.isNotEmpty
                              ? AppColors.getPriorityBgColor(filter['value']!)
                              : (isDark ? AppColors.darkCard : const Color(0xFFF1F5F9)),
                          side: BorderSide(
                            color: isSelected
                                ? (filter['value']!.isNotEmpty
                                    ? AppColors.getPriorityColor(filter['value']!)
                                    : AppColors.primary)
                                : (isDark ? AppColors.darkBorder : AppColors.lightBorder),
                          ),
                        ),
                      );
                    }).toList(),
                  ),
                ),

                Divider(
                  height: 1,
                  color: isDark ? AppColors.darkBorder : AppColors.lightBorder,
                ),
              ],
            ),
          ),

          // Results Count
          if (!provider.isLoading)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              color: isDark ? AppColors.darkBg : AppColors.lightBg,
              child: Text(
                '${provider.allJobs.length} work order${provider.allJobs.length == 1 ? '' : 's'} found',
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted,
                ),
              ),
            ),

          // Job List
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async => _applyFilters(),
              child: provider.isLoading
                  ? const Center(child: CircularProgressIndicator())
                  : provider.error != null
                      ? EmptyState(
                          icon: Icons.error_outline,
                          title: 'Unable to Load Jobs',
                          message: provider.error!,
                          retryButtonText: 'Retry',
                          onRetry: _applyFilters,
                        )
                      : provider.allJobs.isEmpty
                          ? EmptyState(
                              icon: Icons.assignment_outlined,
                              title: 'No jobs found',
                              message: _searchController.text.isNotEmpty || _selectedStatus.isNotEmpty || _selectedPriority.isNotEmpty
                                  ? 'No work orders match the current filters. Try adjusting them.'
                                  : 'You have no assigned work orders at this time.',
                              retryButtonText: _searchController.text.isNotEmpty || _selectedStatus.isNotEmpty || _selectedPriority.isNotEmpty
                                  ? 'Clear Filters'
                                  : null,
                              onRetry: _searchController.text.isNotEmpty || _selectedStatus.isNotEmpty || _selectedPriority.isNotEmpty
                                  ? () {
                                      setState(() {
                                        _selectedStatus = '';
                                        _selectedPriority = '';
                                        _searchController.clear();
                                      });
                                      _applyFilters();
                                    }
                                  : null,
                            )
                          : ListView.builder(
                              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                              itemCount: provider.allJobs.length,
                              itemBuilder: (context, index) {
                                final job = provider.allJobs[index];
                                final isDark = Theme.of(context).brightness == Brightness.dark;

                                return Card(
                                  margin: const EdgeInsets.only(bottom: 10),
                                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                                  clipBehavior: Clip.antiAlias,
                                  child: InkWell(
                                    onTap: () {
                                      Navigator.pushNamed(
                                        context,
                                        AppRouter.jobDetails,
                                        arguments: job.id,
                                      );
                                    },
                                    child: IntrinsicHeight(
                                      child: Row(
                                        crossAxisAlignment: CrossAxisAlignment.stretch,
                                        children: [
                                          // Priority Indicator Bar
                                          Container(
                                            width: 5,
                                            color: AppColors.getPriorityColor(job.priority),
                                          ),

                                          // Content
                                          Expanded(
                                            child: Padding(
                                              padding: const EdgeInsets.all(14),
                                              child: Column(
                                                crossAxisAlignment: CrossAxisAlignment.start,
                                                children: [
                                                  Row(
                                                    children: [
                                                      PriorityBadge(priority: job.priority),
                                                      const SizedBox(width: 8),
                                                      StatusBadge(status: job.status),
                                                      const Spacer(),
                                                      Text(
                                                        job.workOrderNumber,
                                                        style: TextStyle(
                                                          fontSize: 11,
                                                          fontWeight: FontWeight.w700,
                                                          color: isDark ? AppColors.primaryLight : AppColors.primary,
                                                        ),
                                                      ),
                                                    ],
                                                  ),
                                                  const SizedBox(height: 8),
                                                  Text(
                                                    job.title,
                                                    style: TextStyle(
                                                      fontSize: 14,
                                                      fontWeight: FontWeight.w600,
                                                      color: isDark ? AppColors.darkTextPrimary : AppColors.lightTextPrimary,
                                                    ),
                                                    maxLines: 2,
                                                    overflow: TextOverflow.ellipsis,
                                                  ),
                                                  const SizedBox(height: 6),
                                                  Row(
                                                    children: [
                                                      Icon(Icons.location_on_outlined, size: 13,
                                                          color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted),
                                                      const SizedBox(width: 3),
                                                      Expanded(
                                                        child: Text(
                                                          job.formattedLocation,
                                                          style: TextStyle(
                                                            fontSize: 12,
                                                            color: isDark ? AppColors.darkTextSecondary : AppColors.lightTextSecondary,
                                                          ),
                                                          overflow: TextOverflow.ellipsis,
                                                        ),
                                                      ),
                                                    ],
                                                  ),
                                                  const SizedBox(height: 4),
                                                  Row(
                                                    children: [
                                                      Icon(Icons.access_time_outlined, size: 13,
                                                          color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted),
                                                      const SizedBox(width: 3),
                                                      Text(
                                                        job.scheduledStartTime != null
                                                            ? DateFormat('EEE, MMM d • hh:mm a').format(job.scheduledStartTime!)
                                                            : 'No scheduled time',
                                                        style: TextStyle(
                                                          fontSize: 12,
                                                          color: isDark ? AppColors.darkTextSecondary : AppColors.lightTextSecondary,
                                                        ),
                                                      ),
                                                    ],
                                                  ),
                                                ],
                                              ),
                                            ),
                                          ),
                                          // Chevron
                                          Center(
                                            child: Padding(
                                              padding: const EdgeInsets.only(right: 12),
                                              child: Icon(
                                                Icons.chevron_right,
                                                size: 20,
                                                color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted,
                                              ),
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ),
                                );
                              },
                            ),
            ),
          ),
        ],
      ),
    );
  }
}
