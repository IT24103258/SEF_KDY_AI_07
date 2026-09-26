import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../core/routes/app_router.dart';
import '../core/theme/app_colors.dart';
import '../providers/work_order_provider.dart';
import '../widgets/add_note_modal.dart';
import '../widgets/notes_list.dart';
import '../widgets/priority_badge.dart';
import '../widgets/status_badge.dart';

class JobDetailsScreen extends StatefulWidget {
  final String workOrderId;

  const JobDetailsScreen({super.key, required this.workOrderId});

  @override
  State<JobDetailsScreen> createState() => _JobDetailsScreenState();
}

class _JobDetailsScreenState extends State<JobDetailsScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<WorkOrderProvider>().fetchJobDetails(widget.workOrderId);
    });
  }

  void _openAddNoteModal() {
    AddNoteModal.show(
      context,
      onSave: (text) async {
        return await context
            .read<WorkOrderProvider>()
            .addNote(widget.workOrderId, text);
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<WorkOrderProvider>();
    final job = provider.currentJob?.id == widget.workOrderId
        ? provider.currentJob
        : null;
    final isDark = Theme.of(context).brightness == Brightness.dark;

    final isCompleted = job?.status.toLowerCase() == 'completed';
    final isInProgress = job?.status.toLowerCase() == 'inprogress';
    final isScheduled = job?.status.toLowerCase() == 'scheduled' ||
        job?.status.toLowerCase() == 'approved';

    final scheduledDateStr = job?.scheduledStartTime != null
        ? DateFormat('EEEE, MMMM d, yyyy').format(job!.scheduledStartTime!)
        : 'Date not set';

    final startTimeStr = job?.scheduledStartTime != null
        ? DateFormat('hh:mm a').format(job!.scheduledStartTime!)
        : '--:--';
    final endTimeStr = job?.scheduledEndTime != null
        ? DateFormat('hh:mm a').format(job!.scheduledEndTime!)
        : '--:--';

    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          tooltip: 'Back',
          onPressed: () {
            if (Navigator.canPop(context)) {
              Navigator.pop(context);
            } else {
              Navigator.pushReplacementNamed(context, AppRouter.allJobs);
            }
          },
        ),
        title: Text(
          job?.workOrderNumber.isNotEmpty == true
              ? job!.workOrderNumber
              : 'Job Details',
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh Job Details',
            onPressed: () => provider.fetchJobDetails(widget.workOrderId),
          ),
        ],
      ),
      body: provider.isLoading && job == null
          ? const Center(child: CircularProgressIndicator())
          : job == null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.search_off,
                          size: 48, color: Colors.grey),
                      const SizedBox(height: 12),
                      const Text('Work order not found.',
                          style: TextStyle(fontSize: 16)),
                      const SizedBox(height: 12),
                      ElevatedButton(
                        onPressed: () =>
                            provider.fetchJobDetails(widget.workOrderId),
                        child: const Text('Retry'),
                      ),
                    ],
                  ),
                )
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    // Top Overview Card
                    Card(
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(16),
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
                                    fontSize: 12,
                                    fontWeight: FontWeight.w700,
                                    color: isDark
                                        ? AppColors.darkTextMuted
                                        : AppColors.lightTextMuted,
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 12),
                            Text(
                              job.title,
                              style: TextStyle(
                                fontSize: 18,
                                fontWeight: FontWeight.bold,
                                color: isDark
                                    ? AppColors.darkTextPrimary
                                    : AppColors.lightTextPrimary,
                              ),
                            ),
                            const SizedBox(height: 10),
                            Row(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Icon(
                                  Icons.location_on_outlined,
                                  size: 16,
                                  color: isDark
                                      ? AppColors.darkTextMuted
                                      : AppColors.lightTextMuted,
                                ),
                                const SizedBox(width: 6),
                                Expanded(
                                  child: Text(
                                    job.formattedLocation,
                                    style: TextStyle(
                                      fontSize: 13,
                                      color: isDark
                                          ? AppColors.darkTextSecondary
                                          : AppColors.lightTextSecondary,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),

                    // Schedule Card
                    Card(
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: const [
                                Icon(Icons.event_outlined,
                                    color: AppColors.primary, size: 18),
                                SizedBox(width: 8),
                                Text(
                                  'Schedule',
                                  style: TextStyle(
                                      fontSize: 15,
                                      fontWeight: FontWeight.bold),
                                ),
                              ],
                            ),
                            const Divider(height: 20),
                            _buildDetailRow(
                              label: 'Date',
                              value: scheduledDateStr,
                              isDark: isDark,
                            ),
                            const SizedBox(height: 10),
                            _buildDetailRow(
                              label: 'Scheduled Window',
                              value: '$startTimeStr - $endTimeStr',
                              isDark: isDark,
                            ),
                            const SizedBox(height: 10),
                            _buildDetailRow(
                              label: 'Estimated Duration',
                              value: job.estimatedDurationMinutes > 0
                                  ? '${job.estimatedDurationMinutes} minutes'
                                  : 'Not provided',
                              isDark: isDark,
                            ),
                            if (job.slaDeadline != null) ...[
                              const SizedBox(height: 10),
                              _buildDetailRow(
                                label: 'SLA Target Deadline',
                                value: DateFormat('MMM d, hh:mm a')
                                    .format(job.slaDeadline!),
                                isDark: isDark,
                                valueColor: AppColors.high,
                              ),
                            ],
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),

                    // Description Card
                    Card(
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: const [
                                Icon(Icons.description_outlined,
                                    color: AppColors.primary, size: 18),
                                SizedBox(width: 8),
                                Text(
                                  'Description',
                                  style: TextStyle(
                                      fontSize: 15,
                                      fontWeight: FontWeight.bold),
                                ),
                              ],
                            ),
                            const Divider(height: 20),
                            Text(
                              job.description.isNotEmpty
                                  ? job.description
                                  : 'No specific issue description provided.',
                              style: TextStyle(
                                fontSize: 14,
                                height: 1.4,
                                color: isDark
                                    ? AppColors.darkTextSecondary
                                    : const Color(0xFF334155),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),

                    // Status History Card
                    Card(
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: const [
                                Icon(Icons.history_outlined,
                                    color: AppColors.primary, size: 18),
                                SizedBox(width: 8),
                                Text(
                                  'Status History',
                                  style: TextStyle(
                                      fontSize: 15,
                                      fontWeight: FontWeight.bold),
                                ),
                              ],
                            ),
                            const Divider(height: 20),
                            if (job.statusHistories.isEmpty)
                              Padding(
                                padding:
                                    const EdgeInsets.symmetric(vertical: 8),
                                child: Text(
                                  'Current status is ${job.status}. No previous transitions recorded.',
                                  style: TextStyle(
                                    fontSize: 13,
                                    color: isDark
                                        ? AppColors.darkTextMuted
                                        : AppColors.lightTextMuted,
                                  ),
                                ),
                              )
                            else
                              ListView.separated(
                                shrinkWrap: true,
                                physics: const NeverScrollableScrollPhysics(),
                                itemCount: job.statusHistories.length,
                                separatorBuilder: (_, __) =>
                                    const Divider(height: 16),
                                itemBuilder: (context, index) {
                                  final history = job.statusHistories[index];
                                  return Row(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.start,
                                    children: [
                                      Container(
                                        padding: const EdgeInsets.all(4),
                                        decoration: BoxDecoration(
                                          color: AppColors.getStatusBgColor(
                                              history.newStatus),
                                          shape: BoxShape.circle,
                                        ),
                                        child: Icon(
                                          Icons.circle,
                                          size: 8,
                                          color: AppColors.getStatusColor(
                                              history.newStatus),
                                        ),
                                      ),
                                      const SizedBox(width: 10),
                                      Expanded(
                                        child: Column(
                                          crossAxisAlignment:
                                              CrossAxisAlignment.start,
                                          children: [
                                            Row(
                                              mainAxisAlignment:
                                                  MainAxisAlignment
                                                      .spaceBetween,
                                              children: [
                                                Text(
                                                  '${history.previousStatus.isNotEmpty ? '${history.previousStatus} → ' : ''}${history.newStatus}',
                                                  style: const TextStyle(
                                                      fontWeight:
                                                          FontWeight.bold,
                                                      fontSize: 13),
                                                ),
                                                Text(
                                                  DateFormat('MMM d, hh:mm a')
                                                      .format(
                                                          history.timestamp),
                                                  style: TextStyle(
                                                    fontSize: 11,
                                                    color: isDark
                                                        ? AppColors
                                                            .darkTextMuted
                                                        : AppColors
                                                            .lightTextMuted,
                                                  ),
                                                ),
                                              ],
                                            ),
                                            if (history.changedByName
                                                    ?.isNotEmpty ==
                                                true) ...[
                                              const SizedBox(height: 2),
                                              Text(
                                                'Changed by ${history.changedByName}',
                                                style: TextStyle(
                                                  fontSize: 12,
                                                  color: isDark
                                                      ? AppColors
                                                          .darkTextSecondary
                                                      : AppColors
                                                          .lightTextSecondary,
                                                ),
                                              ),
                                            ],
                                            if (history.reason.isNotEmpty) ...[
                                              const SizedBox(height: 2),
                                              Text(
                                                history.reason,
                                                style: TextStyle(
                                                  fontSize: 12,
                                                  color: isDark
                                                      ? AppColors
                                                          .darkTextSecondary
                                                      : AppColors
                                                          .lightTextSecondary,
                                                ),
                                              ),
                                            ],
                                          ],
                                        ),
                                      ),
                                    ],
                                  );
                                },
                              ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),

                    // Field Notes Card
                    Card(
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Row(
                                  children: const [
                                    Icon(Icons.speaker_notes_outlined,
                                        color: AppColors.primary, size: 18),
                                    SizedBox(width: 8),
                                    Text(
                                      'Field Notes',
                                      style: TextStyle(
                                          fontSize: 15,
                                          fontWeight: FontWeight.bold),
                                    ),
                                  ],
                                ),
                                TextButton.icon(
                                  onPressed: _openAddNoteModal,
                                  icon: const Icon(Icons.add, size: 16),
                                  label: const Text('Add Note',
                                      style: TextStyle(fontSize: 12)),
                                ),
                              ],
                            ),
                            const Divider(height: 14),
                            NotesList(notes: job.notes),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 24),
                  ],
                ),
      bottomNavigationBar: job != null &&
              (isCompleted || isInProgress || isScheduled)
          ? Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: isDark ? AppColors.darkCard : Colors.white,
                border: Border(
                  top: BorderSide(
                    color:
                        isDark ? AppColors.darkBorder : AppColors.lightBorder,
                    width: 1,
                  ),
                ),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withOpacity(0.06),
                    blurRadius: 8,
                    offset: const Offset(0, -2),
                  ),
                ],
              ),
              child: SafeArea(
                child: SizedBox(
                  height: 48,
                  child: isCompleted
                      ? OutlinedButton.icon(
                          onPressed: () {
                            Navigator.pushNamed(
                              context,
                              AppRouter.jobExecutionPath(job.id),
                            );
                          },
                          icon: const Icon(Icons.visibility_outlined, size: 18),
                          label: const Text(
                              'View Execution & Completion Evidence'),
                        )
                      : isInProgress
                          ? ElevatedButton.icon(
                              onPressed: () {
                                Navigator.pushNamed(
                                  context,
                                  AppRouter.jobExecutionPath(job.id),
                                );
                              },
                              icon: const Icon(Icons.play_arrow, size: 18),
                              label: const Text('Continue Job Execution'),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: AppColors.inProgress,
                                foregroundColor: Colors.white,
                              ),
                            )
                          : isScheduled
                              ? ElevatedButton.icon(
                                  onPressed: () async {
                                    final nav = Navigator.of(context);
                                    final success =
                                        await provider.startJob(job.id);
                                    if (success && mounted) {
                                      nav.pushNamed(
                                        AppRouter.jobExecutionPath(job.id),
                                      );
                                    }
                                  },
                                  icon: const Icon(Icons.play_arrow_outlined,
                                      size: 18),
                                  label: const Text('Start Job'),
                                  style: ElevatedButton.styleFrom(
                                    backgroundColor: AppColors.primary,
                                    foregroundColor: Colors.white,
                                  ),
                                )
                              : ElevatedButton.icon(
                                  onPressed: () {
                                    Navigator.pushNamed(
                                      context,
                                      AppRouter.jobExecutionPath(job.id),
                                    );
                                  },
                                  icon:
                                      const Icon(Icons.arrow_forward, size: 18),
                                  label: const Text('Open Job Workspace'),
                                ),
                ),
              ),
            )
          : null,
    );
  }

  Widget _buildDetailRow({
    required String label,
    required String value,
    required bool isDark,
    Color? valueColor,
  }) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: TextStyle(
            fontSize: 13,
            color: isDark
                ? AppColors.darkTextSecondary
                : AppColors.lightTextSecondary,
          ),
        ),
        Text(
          value,
          style: TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w600,
            color: valueColor ??
                (isDark
                    ? AppColors.darkTextPrimary
                    : AppColors.lightTextPrimary),
          ),
        ),
      ],
    );
  }
}
