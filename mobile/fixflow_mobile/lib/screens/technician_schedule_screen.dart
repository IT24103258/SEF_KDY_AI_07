import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../providers/work_order_provider.dart';
import '../models/work_order_model.dart';
import '../core/routes/app_router.dart';

class TechnicianScheduleScreen extends StatefulWidget {
  const TechnicianScheduleScreen({super.key});

  @override
  State<TechnicianScheduleScreen> createState() => _TechnicianScheduleScreenState();
}

class _TechnicianScheduleScreenState extends State<TechnicianScheduleScreen> {
  DateTime _selectedDate = DateTime.now();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<WorkOrderProvider>().fetchSchedule(filterDate: _selectedDate);
    });
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<WorkOrderProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Daily Schedule'),
        actions: [
          IconButton(
            icon: const Icon(Icons.list_alt),
            tooltip: 'All Assigned Jobs',
            onPressed: () {
              Navigator.pushNamed(context, AppRouter.allJobs);
            },
          ),
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => provider.fetchSchedule(filterDate: _selectedDate),
          ),
        ],
      ),
      body: Column(
        children: [
          // Horizontal Day Selector Bar
          Container(
            padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 8),
            decoration: BoxDecoration(
              color: Theme.of(context).cardColor,
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.05),
                  blurRadius: 4,
                  offset: const Offset(0, 2),
                ),
              ],
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: List.generate(7, (index) {
                final date = DateTime.now().add(Duration(days: index - 2));
                final isSelected = date.day == _selectedDate.day &&
                    date.month == _selectedDate.month &&
                    date.year == _selectedDate.year;

                return GestureDetector(
                  onTap: () {
                    setState(() {
                      _selectedDate = date;
                    });
                    provider.setSelectedDate(date);
                  },
                  child: Container(
                    padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 10),
                    decoration: BoxDecoration(
                      color: isSelected ? Theme.of(context).primaryColor : Colors.transparent,
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Column(
                      children: [
                        Text(
                          DateFormat('E').format(date),
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: isSelected ? Colors.white : Colors.grey,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          DateFormat('d').format(date),
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: isSelected ? Colors.white : null,
                          ),
                        ),
                      ],
                    ),
                  ),
                );
              }),
            ),
          ),

          // Job list
          Expanded(
            child: provider.isLoading
                ? const Center(child: CircularProgressIndicator())
                : provider.error != null
                    ? Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            const Icon(Icons.error_outline, color: Colors.red, size: 40),
                            const SizedBox(height: 8),
                            Text(provider.error!, style: const TextStyle(color: Colors.red)),
                            const SizedBox(height: 12),
                            ElevatedButton(
                              onPressed: () => provider.fetchSchedule(filterDate: _selectedDate),
                              child: const Text('Retry'),
                            ),
                          ],
                        ),
                      )
                    : provider.scheduleJobs.isEmpty
                        ? Center(
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Icon(Icons.event_available, size: 48, color: Colors.grey[400]),
                                const SizedBox(height: 12),
                                Text(
                                  'No scheduled jobs for ${DateFormat('EEE, MMM d').format(_selectedDate)}',
                                  style: TextStyle(color: Colors.grey[600]),
                                ),
                              ],
                            ),
                          )
                        : ListView.builder(
                            padding: const EdgeInsets.all(12),
                            itemCount: provider.scheduleJobs.length,
                            itemBuilder: (context, index) {
                              final job = provider.scheduleJobs[index];
                              return _buildJobCard(context, job);
                            },
                          ),
          ),
        ],
      ),
    );
  }

  Widget _buildJobCard(BuildContext context, WorkOrderModel job) {
    final isCritical = job.priority.toLowerCase() == 'critical';
    final isHigh = job.priority.toLowerCase() == 'high';
    final hasConflict = job.conflictDetected;

    final startStr = job.scheduledStartTime != null
        ? DateFormat('hh:mm a').format(job.scheduledStartTime!)
        : '09:00 AM';
    final endStr = job.scheduledEndTime != null
        ? DateFormat('hh:mm a').format(job.scheduledEndTime!)
        : '11:00 AM';

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(
          color: hasConflict
              ? Colors.red
              : isCritical
                  ? Colors.red.shade300
                  : isHigh
                      ? Colors.orange.shade300
                      : Colors.grey.shade200,
          width: hasConflict ? 1.5 : 1.0,
        ),
      ),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () {
          Navigator.pushNamed(
            context,
            AppRouter.jobDetails,
            arguments: job.id,
          );
        },
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header row
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      Text(
                        job.workOrderNumber,
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          color: Theme.of(context).primaryColor,
                        ),
                      ),
                      if (hasConflict) ...[
                        const SizedBox(width: 6),
                        const Icon(Icons.warning_amber_rounded, color: Colors.red, size: 16),
                      ],
                    ],
                  ),
                  _buildStatusChip(job.status),
                ],
              ),
              const SizedBox(height: 8),

              // Title
              Text(
                job.title,
                style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
              ),
              const SizedBox(height: 6),

              // Location
              Row(
                children: [
                  const Icon(Icons.location_on_outlined, size: 14, color: Colors.grey),
                  const SizedBox(width: 4),
                  Expanded(
                    child: Text(
                      '${job.locationName} (${job.building}, ${job.room})',
                      style: TextStyle(fontSize: 12, color: Colors.grey[600]),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),

              // Time & Priority
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      const Icon(Icons.access_time, size: 14, color: Colors.grey),
                      const SizedBox(width: 4),
                      Text(
                        '$startStr - $endStr (${job.estimatedDurationMinutes}m)',
                        style: TextStyle(fontSize: 12, color: Colors.grey[700], fontWeight: FontWeight.w500),
                      ),
                    ],
                  ),
                  _buildPriorityTag(job.priority),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildStatusChip(String status) {
    Color bg = Colors.grey.shade100;
    Color fg = Colors.grey.shade800;

    switch (status.toLowerCase()) {
      case 'completed':
        bg = Colors.green.shade50;
        fg = Colors.green.shade700;
        break;
      case 'inprogress':
        bg = Colors.blue.shade50;
        fg = Colors.blue.shade700;
        break;
      case 'scheduled':
      case 'approved':
        bg = Colors.orange.shade50;
        fg = Colors.orange.shade700;
        break;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        status,
        style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: fg),
      ),
    );
  }

  Widget _buildPriorityTag(String priority) {
    Color color = Colors.blue;
    if (priority.toLowerCase() == 'critical') color = Colors.red;
    if (priority.toLowerCase() == 'high') color = Colors.orange;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(6),
        border: Border.all(color: color.withValues(alpha: 0.4)),
      ),
      child: Text(
        priority,
        style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: color),
      ),
    );
  }
}
