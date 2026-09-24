import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/work_order_provider.dart';
import '../core/routes/app_router.dart';

class AllJobsScreen extends StatefulWidget {
  const AllJobsScreen({super.key});

  @override
  State<AllJobsScreen> createState() => _AllJobsScreenState();
}

class _AllJobsScreenState extends State<AllJobsScreen> {
  String _selectedStatus = '';

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<WorkOrderProvider>().fetchAllJobs();
    });
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<WorkOrderProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('All Assigned Jobs'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => provider.fetchAllJobs(status: _selectedStatus.isNotEmpty ? _selectedStatus : null),
          ),
        ],
      ),
      body: Column(
        children: [
          // Filter Chips
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            child: Row(
              children: [
                _buildFilterChip('All', ''),
                _buildFilterChip('Scheduled', 'Scheduled'),
                _buildFilterChip('In Progress', 'InProgress'),
                _buildFilterChip('Completed', 'Completed'),
              ],
            ),
          ),

          // List of jobs
          Expanded(
            child: provider.isLoading
                ? const Center(child: CircularProgressIndicator())
                : provider.error != null
                    ? Center(child: Text(provider.error!, style: const TextStyle(color: Colors.red)))
                    : provider.allJobs.isEmpty
                        ? Center(
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Icon(Icons.assignment_outlined, size: 48, color: Colors.grey[400]),
                                const SizedBox(height: 12),
                                Text('No work orders found.', style: TextStyle(color: Colors.grey[600])),
                              ],
                            ),
                          )
                        : ListView.builder(
                            padding: const EdgeInsets.all(12),
                            itemCount: provider.allJobs.length,
                            itemBuilder: (context, index) {
                              final job = provider.allJobs[index];
                              return Card(
                                margin: const EdgeInsets.only(bottom: 10),
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                                child: ListTile(
                                  onTap: () {
                                    Navigator.pushNamed(
                                      context,
                                      AppRouter.jobDetails,
                                      arguments: job.id,
                                    );
                                  },
                                  title: Text(
                                    job.title,
                                    style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
                                  ),
                                  subtitle: Text(
                                    '${job.workOrderNumber} • ${job.locationName}',
                                    style: TextStyle(fontSize: 12, color: Colors.grey[600]),
                                  ),
                                  trailing: Text(
                                    job.status,
                                    style: TextStyle(
                                      fontSize: 11,
                                      fontWeight: FontWeight.bold,
                                      color: job.status.toLowerCase() == 'completed'
                                          ? Colors.green
                                          : job.status.toLowerCase() == 'inprogress'
                                              ? Colors.blue
                                              : Colors.orange,
                                    ),
                                  ),
                                ),
                              );
                            },
                          ),
          ),
        ],
      ),
    );
  }

  Widget _buildFilterChip(String label, String value) {
    final isSelected = _selectedStatus == value;
    return Padding(
      padding: const EdgeInsets.only(right: 8),
      child: FilterChip(
        label: Text(label),
        selected: isSelected,
        onSelected: (selected) {
          setState(() {
            _selectedStatus = selected ? value : '';
          });
          context.read<WorkOrderProvider>().fetchAllJobs(
                status: _selectedStatus.isNotEmpty ? _selectedStatus : null,
              );
        },
      ),
    );
  }
}
