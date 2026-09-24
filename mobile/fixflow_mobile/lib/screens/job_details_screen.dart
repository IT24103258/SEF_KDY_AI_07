import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../providers/work_order_provider.dart';
import '../core/routes/app_router.dart';

class JobDetailsScreen extends StatefulWidget {
  final String workOrderId;

  const JobDetailsScreen({super.key, required this.workOrderId});

  @override
  State<JobDetailsScreen> createState() => _JobDetailsScreenState();
}

class _JobDetailsScreenState extends State<JobDetailsScreen> {
  final TextEditingController _noteController = TextEditingController();
  bool _isAddingNote = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<WorkOrderProvider>().fetchJobDetails(widget.workOrderId);
    });
  }

  @override
  void dispose() {
    _noteController.dispose();
    super.dispose();
  }

  Future<void> _handleAddNote() async {
    final text = _noteController.text.trim();
    if (text.isEmpty) return;

    setState(() => _isAddingNote = true);
    final success = await context.read<WorkOrderProvider>().addNote(widget.workOrderId, text);
    setState(() => _isAddingNote = false);

    if (success && mounted) {
      _noteController.clear();
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Note added successfully.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<WorkOrderProvider>();
    final job = provider.currentJob;

    return Scaffold(
      appBar: AppBar(
        title: Text(job?.workOrderNumber ?? 'Job Details'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => provider.fetchJobDetails(widget.workOrderId),
          ),
        ],
      ),
      body: provider.isLoading && job == null
          ? const Center(child: CircularProgressIndicator())
          : job == null
              ? const Center(child: Text('Work order not found.'))
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    // Status & Priority Banner
                    Card(
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Text(
                                  job.workOrderNumber,
                                  style: TextStyle(
                                    fontWeight: FontWeight.bold,
                                    fontSize: 16,
                                    color: Theme.of(context).primaryColor,
                                  ),
                                ),
                                Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                  decoration: BoxDecoration(
                                    color: job.status.toLowerCase() == 'inprogress'
                                        ? Colors.blue.shade50
                                        : job.status.toLowerCase() == 'completed'
                                            ? Colors.green.shade50
                                            : Colors.orange.shade50,
                                    borderRadius: BorderRadius.circular(8),
                                  ),
                                  child: Text(
                                    job.status,
                                    style: TextStyle(
                                      fontSize: 12,
                                      fontWeight: FontWeight.bold,
                                      color: job.status.toLowerCase() == 'inprogress'
                                          ? Colors.blue.shade700
                                          : job.status.toLowerCase() == 'completed'
                                              ? Colors.green.shade700
                                              : Colors.orange.shade700,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 10),
                            Text(
                              job.title,
                              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                            ),
                            const SizedBox(height: 6),
                            Text(
                              job.description,
                              style: TextStyle(fontSize: 14, color: Colors.grey[700]),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 16),

                    // Operational Details
                    Card(
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'Location & Schedule',
                              style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                            ),
                            const Divider(height: 20),
                            _buildInfoRow(
                              Icons.location_on_outlined,
                              'Location',
                              '${job.locationName} (${job.building}, ${job.room})',
                            ),
                            const SizedBox(height: 12),
                            _buildInfoRow(
                              Icons.access_time,
                              'Scheduled Time',
                              job.scheduledStartTime != null
                                  ? '${DateFormat('MMM d, hh:mm a').format(job.scheduledStartTime!)} - ${job.scheduledEndTime != null ? DateFormat('hh:mm a').format(job.scheduledEndTime!) : ''}'
                                  : 'Not scheduled',
                            ),
                            const SizedBox(height: 12),
                            _buildInfoRow(
                              Icons.timer_outlined,
                              'Estimated Duration',
                              '${job.estimatedDurationMinutes} minutes',
                            ),
                            if (job.slaDeadline != null) ...[
                              const SizedBox(height: 12),
                              _buildInfoRow(
                                Icons.alarm,
                                'SLA Target',
                                DateFormat('MMM d, hh:mm a').format(job.slaDeadline!),
                                color: Colors.orange.shade700,
                              ),
                            ],
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 16),

                    // Field Notes Section
                    Card(
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'Field Work Notes',
                              style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                            ),
                            const SizedBox(height: 12),
                            Row(
                              children: [
                                Expanded(
                                  child: TextField(
                                    controller: _noteController,
                                    decoration: InputDecoration(
                                      hintText: 'Add field note...',
                                      isDense: true,
                                      border: OutlineInputBorder(
                                        borderRadius: BorderRadius.circular(8),
                                      ),
                                    ),
                                  ),
                                ),
                                const SizedBox(width: 8),
                                ElevatedButton(
                                  onPressed: _isAddingNote ? null : _handleAddNote,
                                  style: ElevatedButton.styleFrom(
                                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                                  ),
                                  child: _isAddingNote
                                      ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                                      : const Icon(Icons.send, size: 16),
                                ),
                              ],
                            ),
                            const SizedBox(height: 12),
                            if (job.notes.isEmpty)
                              Text('No notes recorded yet.', style: TextStyle(color: Colors.grey[500], fontSize: 13))
                            else
                              ...job.notes.map((n) => Container(
                                    margin: const EdgeInsets.only(bottom: 8),
                                    padding: const EdgeInsets.all(10),
                                    decoration: BoxDecoration(
                                      color: Colors.grey.shade50,
                                      borderRadius: BorderRadius.circular(8),
                                      border: Border.all(color: Colors.grey.shade200),
                                    ),
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        Row(
                                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                          children: [
                                            Text(n.authorName, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 12)),
                                            Text(DateFormat('hh:mm a').format(n.timestamp), style: TextStyle(color: Colors.grey[600], fontSize: 11)),
                                          ],
                                        ),
                                        const SizedBox(height: 4),
                                        Text(n.noteText, style: const TextStyle(fontSize: 13)),
                                      ],
                                    ),
                                  )),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
      bottomNavigationBar: job != null && job.status.toLowerCase() != 'completed'
          ? Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: Theme.of(context).cardColor,
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withOpacity(0.08),
                    blurRadius: 6,
                    offset: const Offset(0, -2),
                  ),
                ],
              ),
              child: job.status.toLowerCase() == 'inprogress'
                  ? ElevatedButton.icon(
                      icon: const Icon(Icons.check_circle_outline),
                      label: const Text('Proceed to Execution & Sign-off'),
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        backgroundColor: Colors.green.shade600,
                        foregroundColor: Colors.white,
                      ),
                      onPressed: () {
                        Navigator.pushNamed(
                          context,
                          AppRouter.jobExecution,
                          arguments: job.id,
                        );
                      },
                    )
                  : ElevatedButton.icon(
                      icon: const Icon(Icons.play_arrow),
                      label: const Text('Start Job Execution'),
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 14),
                      ),
                      onPressed: () async {
                        final messenger = ScaffoldMessenger.of(context);
                        final success = await provider.startJob(job.id);
                        if (success && mounted) {
                          messenger.showSnackBar(
                            const SnackBar(content: Text('Job started. Status changed to In Progress.')),
                          );
                        }
                      },
                    ),
            )
          : null,
    );
  }

  Widget _buildInfoRow(IconData icon, String label, String value, {Color? color}) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 18, color: color ?? Colors.grey[600]),
        const SizedBox(width: 8),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label, style: TextStyle(fontSize: 12, color: Colors.grey[600])),
            Text(value, style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: color)),
          ],
        ),
      ],
    );
  }
}
