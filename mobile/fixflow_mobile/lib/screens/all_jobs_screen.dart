import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/routes/app_router.dart';
import '../providers/work_order_provider.dart';
import '../widgets/app_top_bar.dart';
import '../widgets/empty_state.dart';
import '../widgets/job_card.dart';

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

  static const _statusFilters = [
    ('All', ''),
    ('In Progress', 'InProgress'),
    ('Completed', 'Completed'),
  ];

  static const _priorityFilters = [
    ('All priorities', ''),
    ('Critical', 'Critical'),
    ('High', 'High'),
    ('Medium', 'Medium'),
    ('Low', 'Low'),
  ];

  @override
  void initState() {
    super.initState();

    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) {
        _applyFilters();
      }
    });

    _searchController.addListener(_onSearchChanged);
  }

  @override
  void dispose() {
    _searchController
      ..removeListener(_onSearchChanged)
      ..dispose();

    super.dispose();
  }

  void _onSearchChanged() {
    _applyFilters();
  }

  Future<void> _applyFilters() {
    return context.read<WorkOrderProvider>().fetchAllJobs(
          status: _selectedStatus.isEmpty ? null : _selectedStatus,
          search: _searchController.text.trim().isEmpty
              ? null
              : _searchController.text.trim(),
          priority: _selectedPriority.isEmpty ? null : _selectedPriority,
        );
  }

  void _selectStatus(String status) {
    if (_selectedStatus == status) return;

    setState(() {
      _selectedStatus = status;
    });

    _applyFilters();
  }

  Widget _buildStatusChip(String label, String status) {
    final isSelected = _selectedStatus == status;
    final colorScheme = Theme.of(context).colorScheme;

    return ChoiceChip(
      label: Text(label),
      selected: isSelected,
      onSelected: (_) => _selectStatus(status),
      showCheckmark: false,
      selectedColor: colorScheme.primaryContainer,
      backgroundColor: colorScheme.surface,
      labelStyle: TextStyle(
        color:
            isSelected ? colorScheme.onPrimaryContainer : colorScheme.onSurface,
        fontWeight: isSelected ? FontWeight.w600 : FontWeight.w500,
      ),
      side: BorderSide(
        color: isSelected ? colorScheme.primary : colorScheme.outlineVariant,
      ),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
      ),
    );
  }

  void _clearFilters() {
    setState(() {
      _selectedStatus = '';
      _selectedPriority = '';
      _searchController.clear();
    });

    _applyFilters();
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<WorkOrderProvider>();

    final hasFilters = _searchController.text.isNotEmpty ||
        _selectedStatus.isNotEmpty ||
        _selectedPriority.isNotEmpty;

    return Scaffold(
      appBar: AppTopBar(
        title: 'All Jobs',
        onRefresh: _applyFilters,
        additionalActions: [
          IconButton(
            icon: Icon(
              _showSearch ? Icons.search_off_outlined : Icons.search,
            ),
            tooltip: _showSearch ? 'Hide search' : 'Search jobs',
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
          if (_showSearch)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
              child: TextField(
                controller: _searchController,
                autofocus: true,
                decoration: InputDecoration(
                  hintText: 'Search by job number or title',
                  prefixIcon: const Icon(Icons.search),
                  suffixIcon: _searchController.text.isEmpty
                      ? null
                      : IconButton(
                          icon: const Icon(Icons.close),
                          tooltip: 'Clear search',
                          onPressed: _searchController.clear,
                        ),
                ),
              ),
            ),

          // Visible status filter chips.
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  for (var i = 0; i < _statusFilters.length; i++) ...[
                    if (i > 0) const SizedBox(width: 8),
                    _buildStatusChip(
                      _statusFilters[i].$1,
                      _statusFilters[i].$2,
                    ),
                  ],
                ],
              ),
            ),
          ),

          // Keep the existing priority filter.
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
            child: DropdownButtonFormField<String>(
              value: _selectedPriority,
              isDense: true,
              decoration: const InputDecoration(
                labelText: 'Priority',
                prefixIcon: Icon(Icons.flag_outlined),
                border: OutlineInputBorder(),
              ),
              items: _priorityFilters
                  .map(
                    (filter) => DropdownMenuItem<String>(
                      value: filter.$2,
                      child: Text(filter.$1),
                    ),
                  )
                  .toList(),
              onChanged: (value) {
                setState(() {
                  _selectedPriority = value ?? '';
                });

                _applyFilters();
              },
            ),
          ),

          Expanded(
            child: RefreshIndicator(
              onRefresh: _applyFilters,
              child: provider.isLoading
                  ? const Center(
                      child: CircularProgressIndicator(),
                    )
                  : provider.error != null
                      ? EmptyState(
                          icon: Icons.error_outline,
                          title: 'Unable to load jobs',
                          message: provider.error!,
                          retryButtonText: 'Retry',
                          onRetry: _applyFilters,
                        )
                      : provider.allJobs.isEmpty
                          ? EmptyState(
                              icon: Icons.assignment_outlined,
                              title: 'No jobs found',
                              message: hasFilters
                                  ? 'No work orders match the current filters.'
                                  : 'You have no assigned work orders at this time.',
                              retryButtonText:
                                  hasFilters ? 'Clear filters' : null,
                              onRetry: hasFilters ? _clearFilters : null,
                            )
                          : ListView.builder(
                              padding: const EdgeInsets.fromLTRB(
                                16,
                                4,
                                16,
                                16,
                              ),
                              itemCount: provider.allJobs.length,
                              itemBuilder: (context, index) {
                                final job = provider.allJobs[index];

                                return JobCard(
                                  job: job,
                                  showActions: false,
                                  onDetails: () => Navigator.pushNamed(
                                    context,
                                    AppRouter.jobDetailsPath(
                                      job.id,
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
