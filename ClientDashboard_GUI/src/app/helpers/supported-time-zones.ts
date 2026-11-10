// Mirrors ClientDashboard_API/Helpers/SupportedTimeZones.cs - keep both lists in sync.
// The API rejects any time zone that isn't in its list.

export interface TimeZoneOption {
  label: string;
  value: string;
}

export const DEFAULT_TIME_ZONE_ID = 'Europe/London';

const SUPPORTED_TIME_ZONES = [
  { city: 'London', id: 'Europe/London' },
  { city: 'Dublin', id: 'Europe/Dublin' },
  { city: 'Lisbon', id: 'Europe/Lisbon' },
  { city: 'Madrid', id: 'Europe/Madrid' },
  { city: 'Paris', id: 'Europe/Paris' },
  { city: 'Amsterdam', id: 'Europe/Amsterdam' },
  { city: 'Berlin', id: 'Europe/Berlin' },
  { city: 'Rome', id: 'Europe/Rome' },
  { city: 'Warsaw', id: 'Europe/Warsaw' },
  { city: 'Athens', id: 'Europe/Athens' },
  { city: 'Dubai', id: 'Asia/Dubai' },
  { city: 'New York', id: 'America/New_York' },
  { city: 'Chicago', id: 'America/Chicago' },
  { city: 'Denver', id: 'America/Denver' },
  { city: 'Los Angeles', id: 'America/Los_Angeles' },
  { city: 'Toronto', id: 'America/Toronto' },
  { city: 'Sydney', id: 'Australia/Sydney' },
  { city: 'Auckland', id: 'Pacific/Auckland' }
];

// Labels like "Warsaw (GMT+1)". The offset is worked out for today, so it follows daylight saving.
export function getTimeZoneOptions(): TimeZoneOption[] {
  return SUPPORTED_TIME_ZONES.map(zone => ({
    label: `${zone.city} (${getCurrentOffset(zone.id)})`,
    value: zone.id
  }));
}

// The device's time zone when we support it, otherwise London
export function getDefaultTimeZoneId(): string {
  const browserTimeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone;
  const isSupported = SUPPORTED_TIME_ZONES.some(zone => zone.id === browserTimeZoneId);

  return isSupported ? browserTimeZoneId : DEFAULT_TIME_ZONE_ID;
}

function getCurrentOffset(timeZoneId: string): string {
  const parts = new Intl.DateTimeFormat('en-GB', { timeZone: timeZoneId, timeZoneName: 'shortOffset' })
    .formatToParts(new Date());
  const offset = parts.find(part => part.type === 'timeZoneName')?.value ?? 'GMT';

  // shortOffset gives plain "GMT" for a zero offset
  return offset === 'GMT' ? 'GMT+0' : offset;
}
