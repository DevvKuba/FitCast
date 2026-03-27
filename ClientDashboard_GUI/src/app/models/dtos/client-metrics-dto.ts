export interface ClientMetricsDto {
  baseClients: number;
  acquiredClients: number;
  acquisitionPercentage: number;
  churnedClients: number;
  churnPercentage: number;
  netGrowth: number;
  netGrowthPercentage: number;
  sessionsPerClient: number;
  monthlyClientSessions: number;
}